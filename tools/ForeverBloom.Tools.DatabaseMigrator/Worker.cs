using System.Diagnostics;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ForeverBloom.Tools.DatabaseMigrator;

internal sealed class Worker : BackgroundService
{
    public const string ActivitySourceName = "DatabaseMigrator";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private const int SuccessExitCode = 0;
    private const int ErrorExitCode = 1;

    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly DatabaseMigratorSettings _settings;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<DatabaseMigratorSettings> settings,
        ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var exitCode = ErrorExitCode;
        using var activity = ActivitySource.StartActivity("Database migration workflow", ActivityKind.Client);

        try
        {
            _logger.LogInformation("Database migration workflow starting");

            using var scope = _serviceProvider.CreateScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogDebug("Checking for pending migrations");
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

            if (pendingMigrations.Length == 0)
            {
                _logger.LogDebug("No pending migrations");
                exitCode = SuccessExitCode;
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            else if (!_settings.ApplyPendingMigrations)
            {
                const string reason = "ApplyPendingMigrations is false. Database is behind and manual intervention is required.";
                _logger.LogError("{Reason} Pending migrations: {PendingMigrations}", reason, string.Join(", ", pendingMigrations));
                activity?.SetStatus(ActivityStatusCode.Error, reason);
            }
            else
            {
                _logger.LogInformation("Pending migrations detected: {PendingMigrations}", string.Join(", ", pendingMigrations));
                _logger.LogInformation("ApplyPendingMigrations is true. Applying all pending migrations.");
                await dbContext.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Applied all pending migrations successfully");
                exitCode = SuccessExitCode;
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            if (exitCode == SuccessExitCode)
            {
                _logger.LogInformation("Database migration workflow completed");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Database migration workflow was cancelled");
            activity?.SetStatus(ActivityStatusCode.Error, "Cancelled");
            exitCode = ErrorExitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled error occurred during database migration workflow");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            exitCode = ErrorExitCode;
        }
        finally
        {
            Environment.ExitCode = exitCode;
            _logger.LogInformation("Database migrator exiting with code {ExitCode}", exitCode);
            _hostApplicationLifetime.StopApplication();
        }
    }

}
