using ForeverBloom.Aspire.ServiceDefaults;
using ForeverBloom.Persistence;
using ForeverBloom.Tools.DatabaseMigrator;
using Serilog;
using Serilog.Exceptions;

// Create bootstrap logger to log startup messages
Log.Logger = new LoggerConfiguration()
  .WriteTo.Console()
  .CreateBootstrapLogger();

Log.Information("Starting host setup...");

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

    builder.Services.AddOptions<DatabaseMigratorSettings>()
        .BindConfiguration(DatabaseMigratorSettings.ConfigurationSectionName);

    builder.AddServiceDefaults();

    builder.Services.AddHostedService<Worker>();

    builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

    builder.Services.AddApplicationDbContext(builder.Configuration);

    var host = builder.Build();

    host.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
