using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace ForeverBloom.Tools.DatabaseSeeder;

internal sealed class Worker : BackgroundService
{
    public const string ActivitySourceName = "DatabaseSeeder";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private const int SuccessExitCode = 0;
    private const int ErrorExitCode = 1;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly DatabaseSeederSettings _settings;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceScopeFactory serviceScopeFactory,
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<DatabaseSeederSettings> settings,
        ILogger<Worker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _hostApplicationLifetime = hostApplicationLifetime;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var exitCode = ErrorExitCode;
        using var activity = ActivitySource.StartActivity("Database seeding workflow", ActivityKind.Client);

        try
        {
            _logger.LogInformation("Database seeding workflow starting");

            if (!_settings.EnableSeeding)
            {
                _logger.LogInformation("DatabaseSeeder.EnableSeeding is false. Skipping seeding workflow.");
                activity?.SetStatus(ActivityStatusCode.Ok);
                exitCode = SuccessExitCode;
                return;
            }

            await using var scope = _serviceScopeFactory.CreateAsyncScope();

            await RunSeedingAsync(scope.ServiceProvider, stoppingToken);

            _logger.LogInformation("Database seeding workflow completed");
            activity?.SetStatus(ActivityStatusCode.Ok);
            exitCode = SuccessExitCode;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Database seeding was cancelled");
            activity?.SetStatus(ActivityStatusCode.Error, "Cancelled");
            exitCode = ErrorExitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while executing the database seeder");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            exitCode = ErrorExitCode;
        }
        finally
        {
            Environment.ExitCode = exitCode;
            _logger.LogInformation("Database seeder exiting with code {ExitCode}", exitCode);
            _hostApplicationLifetime.StopApplication();
        }
    }

    private async Task RunSeedingAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var seedingActivity = ActivitySource.StartActivity("Catalog seeding", ActivityKind.Internal);

        try
        {
            var catalogSeeder = serviceProvider.GetRequiredService<Seeders.CatalogSeeder>();

            _logger.LogInformation("Running catalog seeding operation");
            await catalogSeeder.SeedAsync(cancellationToken);
            _logger.LogInformation("Catalog seeding operation completed successfully");

            seedingActivity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catalog seeding failed with an error");
            seedingActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            seedingActivity?.AddException(ex);
            throw;
        }
    }
}
