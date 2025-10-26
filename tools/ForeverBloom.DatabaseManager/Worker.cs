using System.Diagnostics;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ForeverBloom.DatabaseManager;

internal class Worker : BackgroundService
{
    public const string ActivitySourceName = "DbManager";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);
    private const int SuccessExitCode = 0;
    private const int ErrorExitCode = 1;

    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly DatabaseManagerSettings _settings;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<DatabaseManagerSettings> settings,
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
        using var activity = s_activitySource.StartActivity("Database setup", ActivityKind.Client);

        try
        {
            _logger.LogInformation("Database setup process starting");

            using var scope = _serviceProvider.CreateScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (_settings.SkipInitialization)
            {
                _logger.LogInformation("Database initialization skipped");
            }
            else
            {
                await EnsureDatabaseInitializedAsync(dbContext, cancellationToken);
                _logger.LogInformation("Ensured database is initialized");
            }

            if (_settings.SkipMigration)
            {
                _logger.LogInformation("Database migration skipped");
            }
            else
            {
                await EnsureMigrationsAppliedAsync(dbContext, cancellationToken);
                _logger.LogInformation("Ensured all migrations are applied");
            }

            if (_settings.SkipSeeding)
            {
                _logger.LogInformation("Database seeding skipped");
            }
            else
            {
                var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                await dataSeeder.SeedAsync(cancellationToken);
                _logger.LogInformation("Ensured database is seeded");
            }

            _logger.LogInformation("Database setup process completed");
            activity?.SetStatus(ActivityStatusCode.Ok);
            exitCode = SuccessExitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled error occurred during database setup");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            exitCode = ErrorExitCode;
        }
        finally
        {
            Environment.ExitCode = exitCode;
            _logger.LogInformation("Database setup process exiting with code {ExitCode}", exitCode);
            _hostApplicationLifetime.StopApplication();
        }
    }

    private async Task EnsureDatabaseInitializedAsync(ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        const string businessSchema = ApplicationDbContext.BusinessSchema;

        const string businessSchemaIdentifier = $"\"{businessSchema}\"";
        var appRoleIdentifier = $"\"{_settings.AppRole}\"";
        var appUserNameIdentifier = $"\"{_settings.AppUserName}\"";

        var appRoleLiteral = $"'{_settings.AppRole.Replace("'", "''")}'";
        var appUserNameLiteral = $"'{_settings.AppUserName.Replace("'", "''")}'";
        var appUserPasswordLiteral = $"'{_settings.AppUserPassword.Replace("'", "''")}'";

        try
        {
            _logger.LogDebug("Ensuring schemas exist");
            const string ensureSchemasScript =
                $"""
                 CREATE SCHEMA IF NOT EXISTS {businessSchemaIdentifier};
                 """;
            await dbContext.Database.ExecuteSqlRawAsync(ensureSchemasScript, cancellationToken);

            _logger.LogDebug("Ensuring application role exists: {AppRole}", _settings.AppRole);
            var ensureRoleScript =
                $"""
                 DO $$
                 BEGIN
                     IF NOT EXISTS (SELECT FROM "pg_catalog"."pg_roles" WHERE "rolname" = {appRoleLiteral}) THEN
                         CREATE ROLE {appRoleIdentifier} NOLOGIN;
                     END IF;
                 END $$;
                 """;
            await dbContext.Database.ExecuteSqlRawAsync(ensureRoleScript, cancellationToken);

            _logger.LogDebug("Ensuring application user exists: {AppUser}", _settings.AppUserName);
            var ensureUserScript =
                $"""
                 DO $$
                 BEGIN
                     IF NOT EXISTS (SELECT FROM "pg_catalog"."pg_user" WHERE "usename" = {appUserNameLiteral}) THEN
                         CREATE USER {appUserNameIdentifier} WITH PASSWORD {appUserPasswordLiteral};
                     END IF;
                 END $$;
                 """;
            await dbContext.Database.ExecuteSqlRawAsync(ensureUserScript, cancellationToken);

            _logger.LogDebug("Ensuring role can connect to the database");
            var ensureRoleCanConnectScript =
                $"""
                 DO $_$
                     DECLARE
                         database TEXT := CURRENT_DATABASE();
                     BEGIN
                         EXECUTE FORMAT('GRANT CONNECT ON DATABASE %I TO {appRoleIdentifier}', database);
                     END
                 $_$;
                 """;
            await dbContext.Database.ExecuteSqlRawAsync(ensureRoleCanConnectScript, cancellationToken);

            _logger.LogDebug("Ensuring user is granted application role");
            var ensureUserHasRoleScript = $"GRANT {appRoleIdentifier} TO {appUserNameIdentifier};";
            await dbContext.Database.ExecuteSqlRawAsync(ensureUserHasRoleScript, cancellationToken);

            _logger.LogDebug("Ensuring role is granted schema usage");
            var ensureRoleCanUseSchemasScript =
                $"""
                 GRANT USAGE ON SCHEMA {businessSchemaIdentifier} TO {appRoleIdentifier};
                 """;
            await dbContext.Database.ExecuteSqlRawAsync(ensureRoleCanUseSchemasScript, cancellationToken);

            _logger.LogDebug("Ensuring default privileges for role are set in application schemas");
            var ensureDefaultPrivilegesScript =
                $"""
                 ALTER DEFAULT PRIVILEGES IN SCHEMA {businessSchemaIdentifier}
                     GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {appRoleIdentifier};
                 ALTER DEFAULT PRIVILEGES IN SCHEMA {businessSchemaIdentifier}
                     GRANT USAGE, SELECT ON SEQUENCES TO {appRoleIdentifier};
                 GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {businessSchemaIdentifier} TO {appRoleIdentifier};
                 GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA {businessSchemaIdentifier} TO {appRoleIdentifier};
                 """;
            await dbContext.Database.ExecuteSqlRawAsync(ensureDefaultPrivilegesScript, cancellationToken);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while executing the setup script");
            throw;
        }
    }

    private async Task EnsureMigrationsAppliedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking if database needs to be migrated");
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pendingMigrations.Length == 0)
        {
            _logger.LogDebug("No pending migrations");
            return;
        }
        _logger.LogDebug("Pending migrations: {PendingMigrations}",
            string.Join(", ", pendingMigrations));

        _logger.LogDebug("Applying all pending migrations");
        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while applying pending migrations");
            throw;
        }
    }
}
