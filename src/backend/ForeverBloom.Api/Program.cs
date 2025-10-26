using FluentValidation;
using ForeverBloom.Api.Authentication;
using ForeverBloom.Api.Authentication.Settings;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Aspire.ServiceDefaults;
using ForeverBloom.Api.Endpoints.Catalog.Categories;
using ForeverBloom.Api.Endpoints.Catalog.Products;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Exceptions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// Create a bootstrap logger to log startup messages
var loggerConfiguration = new LoggerConfiguration().WriteTo.Console();
if (args.Contains("--environment=Testing"))
{
    // Bootstrap logger is not thread-safe and causes errors when running multiple integration tests in parallel
    // This is a workaround to prevent that
    Log.Logger = loggerConfiguration.CreateLogger();
}
else
{
    Log.Logger = loggerConfiguration.CreateBootstrapLogger();
}

Log.Information("Starting host setup...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 1024 * 1024; // 1 MB
    });

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithExceptionDetails();

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            configuration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
            });
        }
    });

    // Add service defaults & Aspire client integrations.
    builder.AddServiceDefaults();

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("db");

    builder.Services.AddRequestTimeouts();
    builder.Services.AddOutputCache();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ILogger>(serviceProvider =>
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;

        var endpointName = httpContext?.GetEndpoint()?.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName;
        if (!string.IsNullOrWhiteSpace(endpointName))
        {
            return loggerFactory.CreateLogger($"Endpoints:{endpointName}");
        }

        var fallbackCategory = httpContext is null
            ? "Endpoints:Unknown"
            : $"Endpoints:{httpContext.Request.Method} {httpContext.Request.Path}";

        return loggerFactory.CreateLogger(fallbackCategory);
    });

    // Add services to the containers
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            ProblemDetailsHelper.EnrichProblemDetails(context.ProblemDetails, context.HttpContext);
        };
    });

    // Configure JSON serialization for Optional<T> support
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new OptionalConverterFactory());
        options.SerializerOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());
    });

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddSingleton(TimeProvider.System);

    // Add database context
    builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' not found during DbContext registration.");
        options.UseNpgsql(postgresConnectionString);
    });

    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ISlugRegistryRepository, SlugRegistryRepository>();

    builder.Services.AddCategoryEndpoints();
    builder.Services.AddProductEndpoints();

    builder.Services.AddDataProtection();

    builder.Services.AddValidatorsFromAssemblyContaining<ForeverBloom.Api.Program>();

    builder.Services.AddSingleton<IValidateOptions<ApiKeyAuthenticationSettings>, ApiKeyAuthenticationSettingsValidator>();
    builder.Services.AddOptions<ApiKeyAuthenticationSettings>()
        .BindConfiguration(ApiKeyAuthenticationSettings.ConfigurationKeyName)
        .ValidateOnStart();

    builder.Services.AddAuthentication(ApiKeyAuthenticationDefaults.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.SchemeName, null);

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(ApiKeyAuthenticationDefaults.FrontendAccessPolicyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("Scope", ApiKeyAuthenticationDefaults.FrontendScope, ApiKeyAuthenticationDefaults.AdminScope);
        });

        options.AddPolicy(ApiKeyAuthenticationDefaults.AdminAccessPolicyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("Scope", ApiKeyAuthenticationDefaults.AdminScope);
        });

        options.FallbackPolicy = options.GetPolicy(ApiKeyAuthenticationDefaults.FrontendAccessPolicyName);
    });

    var app = builder.Build();

    // Correlation Id header - keep it tiny & safe
    app.Use(async (ctx, next) =>
    {
        var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? ctx.TraceIdentifier;
        ctx.Response.OnStarting(() =>
        {
            if (!ctx.Response.Headers.ContainsKey("X-Request-ID"))
                ctx.Response.Headers.Append("X-Request-ID", traceId);
            return Task.CompletedTask;
        });
        await next();
    });

    // Tuned request logging (skip health/docs/metrics)
    app.UseWhen(
        ctx =>
            !ctx.Request.Path.StartsWithSegments("/alive") &&
            !ctx.Request.Path.StartsWithSegments("/health") &&
            !ctx.Request.Path.StartsWithSegments("/openapi") &&
            !ctx.Request.Path.StartsWithSegments("/scalar") &&
            !ctx.Request.Path.StartsWithSegments("/metrics"),
        branch => branch.UseSerilogRequestLogging(opts =>
        {
            opts.IncludeQueryInRequestPath = false;
            opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} -> {StatusCode} in {Elapsed:0.0000} ms";

            opts.GetLevel = (httpContext, _, ex) =>
            {
                if (ex != null) return Serilog.Events.LogEventLevel.Error;
                var sc = httpContext.Response.StatusCode;
                if (sc >= 500) return Serilog.Events.LogEventLevel.Error;
                if (sc >= 400) return Serilog.Events.LogEventLevel.Warning;
                return Serilog.Events.LogEventLevel.Information;
            };

            opts.EnrichDiagnosticContext = (diag, ctx) =>
            {
                var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? ctx.TraceIdentifier;
                var endpointName = ctx.GetEndpoint()?.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName;
                var scope = ctx.User.FindFirst("Scope")?.Value;

                diag.Set("TraceId", traceId);
                if (!string.IsNullOrWhiteSpace(endpointName)) diag.Set("EndpointName", endpointName);
                if (!string.IsNullOrWhiteSpace(scope)) diag.Set("Scope", scope);
                diag.Set("Method", ctx.Request.Method);
                diag.Set("Path", ctx.Request.Path);
                diag.Set("StatusCode", ctx.Response.StatusCode);
            };
        })
    );

    app.UseRequestTimeouts();
    app.UseOutputCache();

    // Configure the HTTP request pipeline.
    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Integration"))
    {
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(options =>
        {
            options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        }).AllowAnonymous();
    }

    app.MapDefaultEndpoints();

    var apiV1Group = app.MapGroup("/api/v1").EnrichProblemDetails();

    apiV1Group.MapCategoryEndpoints();
    apiV1Group.MapProductEndpoints();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

// This fixes the inconsistent accessibility issue in test projects
// ReSharper disable once ClassNeverInstantiated.Global
namespace ForeverBloom.Api
{
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class Program;
}
