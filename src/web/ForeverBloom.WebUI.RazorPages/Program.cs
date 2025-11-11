using System.Globalization;
using ForeverBloom.Aspire.ServiceDefaults;
using ForeverBloom.WebApi.Client;
using ForeverBloom.WebApi.Client.Settings;
using ForeverBloom.WebUI.RazorPages.Settings;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Exceptions;

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

    // Register GoogleAnalyticsSettings
    builder.Services.AddSingleton<IValidateOptions<GoogleAnalyticsSettings>, GoogleAnalyticsSettingsValidator>();
    builder.Services.AddOptions<GoogleAnalyticsSettings>()
        .BindConfiguration(GoogleAnalyticsSettings.ConfigurationKeyName)
        .ValidateOnStart();

    // Register ApiClientSettings
    builder.Services.AddSingleton<IValidateOptions<ApiClientSettings>, ApiClientSettingsValidator>();
    builder.Services.AddOptions<ApiClientSettings>()
      .BindConfiguration(ApiClientSettings.ConfigurationKeyName)
      .ValidateOnStart();

    // Register ApiClient
    builder.Services.AddHttpClient<IApiClient, ApiClient>((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<ApiClientSettings>>().Value;

        client.BaseAddress = new Uri(settings.BasePath);
        client.DefaultRequestHeaders.Add(settings.ApiKeyHeaderName, settings.ApiKey);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

    // Add service defaults & Aspire client integrations.
    builder.AddServiceDefaults();

    // Add services to the container.
    builder.Services.AddRazorPages();

    // Add localization
    builder.Services.AddLocalization();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Setup localization
    var plCulture = new CultureInfo("pl-PL");
    var supportedCultures = new[] { plCulture };
    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(plCulture),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures
    });

    CultureInfo.DefaultThreadCurrentCulture = plCulture;
    CultureInfo.DefaultThreadCurrentUICulture = plCulture;

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // Add strong noindex signal on non-production
    app.Use(async (ctx, next) =>
    {
        if (!app.Environment.IsProduction())
        {
            ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        }
        await next();
    });

    // Configure the content type provider to recognize .avif files
    app.UseStaticFiles(new StaticFileOptions
    {
        ContentTypeProvider = new FileExtensionContentTypeProvider
        {
            Mappings = { [".avif"] = "image/avif" }
        }
    });

    app.UseRouting();

    app.UseAuthorization();

    // Environment-gated robots.txt
    app.MapGet("/robots.txt", (IWebHostEnvironment env) =>
      env.IsProduction()
        ? Results.Text("User-agent: *\nAllow: /\nSitemap: https://foreverbloomstudio.pl/sitemap.xml", "text/plain")
        : Results.Text("User-agent: *\nDisallow: /\n", "text/plain")
    );

    app.MapRazorPages()
       .WithStaticAssets();

    app.MapDefaultEndpoints();

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
