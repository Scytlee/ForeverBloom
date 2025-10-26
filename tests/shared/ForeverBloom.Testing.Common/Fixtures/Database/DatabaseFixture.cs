using ForeverBloom.Persistence.Context;
using ForeverBloom.Testing.Common.Config;
using ForeverBloom.Testing.Common.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace ForeverBloom.Testing.Common.Fixtures.Database;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string UserName = "testuser";
    private const string Password = "testpassword";
    private const string AdminDatabaseName = "postgres";
    private const string TestDatabasePrefix = "test_";

    private static TestInfrastructureSettings Settings => TestInfrastructureConcurrencyGate.Settings;

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17.5-alpine")
        .WithUsername(UserName)
        .WithPassword(Password)
        .WithDatabase(AdminDatabaseName)
        .Build();

    private string? _adminConnectionString;
    private int _mappedPort;

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _mappedPort = _dbContainer.GetMappedPublicPort(5432);
        _adminConnectionString = BuildConnectionString(AdminDatabaseName);

        await EnsureTemplateDatabaseAsync();
        await MigrateTemplateDatabaseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    private async Task EnsureTemplateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        var templateDatabase = Settings.TemplateDatabase;

        await using (var existsCommand = connection.CreateCommand())
        {
            existsCommand.CommandText =
                "SELECT 1 FROM pg_database WHERE datname = @databaseName";
            existsCommand.Parameters.AddWithValue("databaseName", templateDatabase);

            var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;
            if (exists)
            {
                return;
            }
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{EscapeIdentifier(templateDatabase)}\"";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MigrateTemplateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var templateConnectionString = BuildConnectionString(Settings.TemplateDatabase, enablePooling: false);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(templateConnectionString);

        await using var dbContext = new ApplicationDbContext(optionsBuilder.Options);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public async Task<string> CreateTestDatabaseFromTemplateAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var databaseName = GenerateDatabaseName(testId);

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var createDatabaseCommand = connection.CreateCommand())
        {
            createDatabaseCommand.CommandText =
                $"CREATE DATABASE \"{EscapeIdentifier(databaseName)}\" TEMPLATE \"{EscapeIdentifier(Settings.TemplateDatabase)}\"";
            await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await SetDatabaseConnectionLimitAsync(connection, databaseName, cancellationToken);

        return databaseName;
    }

    private string GenerateDatabaseName(Guid testId)
    {
        var testFragment = testId.ToString("N")[..8];
        var suffix = Guid.NewGuid().ToString("N")[..16];
        return $"{TestDatabasePrefix}{testFragment}_{suffix}".ToLowerInvariant();
    }

    private async Task SetDatabaseConnectionLimitAsync(NpgsqlConnection adminConnection, string databaseName, CancellationToken cancellationToken)
    {
        await using var connectionLimitCommand = adminConnection.CreateCommand();
        connectionLimitCommand.CommandText =
            $"ALTER DATABASE \"{EscapeIdentifier(databaseName)}\" WITH CONNECTION LIMIT {Settings.MaxPoolSize}";
        await connectionLimitCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public string BuildTestDbConnectionString(string databaseName)
    {
        EnsureInitialized();
        return BuildConnectionString(databaseName, Settings.MaxPoolSize);
    }

    public async Task DropTestDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await TerminateActiveSessionsAsync(connection, databaseName, cancellationToken);

        await using var dropDatabaseCommand = connection.CreateCommand();
        dropDatabaseCommand.CommandText =
            $"DROP DATABASE IF EXISTS \"{EscapeIdentifier(databaseName)}\"";
        await dropDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private string BuildConnectionString(string databaseName, int? maxPoolSize = null, bool enablePooling = true)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = _mappedPort,
            Username = UserName,
            Password = Password,
            Database = databaseName,
            Pooling = enablePooling,
            IncludeErrorDetail = true
        };

        if (maxPoolSize.HasValue)
        {
            builder.MaxPoolSize = maxPoolSize.Value;
        }

        return builder.ConnectionString;
    }

    private static async Task TerminateActiveSessionsAsync(NpgsqlConnection adminConnection, string databaseName, CancellationToken cancellationToken)
    {
        await using var terminateCommand = adminConnection.CreateCommand();
        terminateCommand.CommandText =
            """
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = @databaseName
              AND pid <> pg_backend_pid();
            """;
        terminateCommand.Parameters.AddWithValue("databaseName", databaseName);
        await terminateCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string EscapeIdentifier(string identifier) => identifier.Replace("\"", "\"\"");

    private void EnsureInitialized()
    {
        if (_adminConnectionString is null)
        {
            throw new InvalidOperationException("Database fixture has not been initialized.");
        }
    }
}
