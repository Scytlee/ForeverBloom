using ForeverBloom.Aspire.ServiceDefaults;
using ForeverBloom.DatabaseManager;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

    builder.Services.AddOptions<DatabaseManagerSettings>()
      .BindConfiguration(DatabaseManagerSettings.ConfigurationKeyName)
      .ValidateOnStart();

    builder.Services.AddSingleton<IValidateOptions<DatabaseManagerSettings>, DatabaseManagerSettingsValidator>();

    builder.AddServiceDefaults();

    builder.Services.AddHostedService<Worker>();

    builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

    var connectionString = builder.Configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string not found");

    builder.Services.AddSingleton(TimeProvider.System);

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
    });

    builder.Services.AddScoped<DataSeeder>();

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
