using ForeverBloom.Application;
using ForeverBloom.Aspire.ServiceDefaults;
using ForeverBloom.Infrastructure;
using ForeverBloom.Persistence;
using ForeverBloom.Tools.DatabaseSeeder;
using ForeverBloom.Tools.DatabaseSeeder.Seeders;
using Serilog;
using Serilog.Exceptions;

// Bootstrap logger to capture startup errors before Serilog is fully configured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting database seeder host setup...");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((serviceProvider, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(serviceProvider)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithExceptionDetails();

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            loggerConfiguration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
            });
        }
    });

    builder.AddServiceDefaults();

    builder.Services.AddOptions<DatabaseSeederSettings>()
        .BindConfiguration(DatabaseSeederSettings.ConfigurationSectionName);

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure();
    builder.Services.AddPersistence(builder.Configuration);

    // Register seeding services
    builder.Services.AddScoped<CatalogSeeder>();

    builder.Services.AddHostedService<Worker>();

    builder.Services
        .AddOpenTelemetry()
        .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

    var host = builder.Build();
    host.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Database seeder host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
