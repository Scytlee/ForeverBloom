using Npgsql;
using Testcontainers.PostgreSql;
using ForeverBloom.Testing.Integration.Config;

namespace ForeverBloom.Testing.Integration.Database;

public sealed class PostgresTemplateDatabaseManager : IAsyncDisposable
{
    private const string UserName = "testuser";
    private const string Password = "testpassword";
    private const string AdminDatabaseName = "postgres";
    private const string PostgresImage = "postgres:17.5-alpine";
    private const int PostgresPort = 5432;

    private readonly TestInfrastructureSettings _settings;
    private readonly PostgreSqlContainer _dbContainer;

    private string? _adminConnectionString;
    private int _mappedPort;
    private string? _host;
    private bool _initialized;

    public PostgresTemplateDatabaseManager(TestInfrastructureSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _dbContainer = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithUsername(UserName)
            .WithPassword(Password)
            .WithDatabase(AdminDatabaseName)
            .Build();
    }

    public async Task InitializeAsync(
        Func<string, CancellationToken, Task> migrateTemplateDatabaseAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(migrateTemplateDatabaseAsync);

        if (_initialized)
        {
            return;
        }

        await _dbContainer.StartAsync(cancellationToken);

        _host = _dbContainer.Hostname;
        _mappedPort = _dbContainer.GetMappedPublicPort(PostgresPort);
        _adminConnectionString = BuildConnectionString(AdminDatabaseName, enablePooling: false);

        await EnsureTemplateDatabaseAsync(cancellationToken);

        var templateConnectionString = BuildConnectionString(_settings.TemplateDatabase, enablePooling: false);
        await migrateTemplateDatabaseAsync(templateConnectionString, cancellationToken);

        _initialized = true;
    }

    public async Task<TestDatabaseInstance> CreateDatabaseAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var databaseName = GenerateDatabaseName(testId);

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var createDatabaseCommand = connection.CreateCommand())
        {
            createDatabaseCommand.CommandText =
                $"CREATE DATABASE \"{EscapeIdentifier(databaseName)}\" TEMPLATE \"{EscapeIdentifier(_settings.TemplateDatabase)}\"";
            await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await SetDatabaseConnectionLimitAsync(connection, databaseName, _settings.MaxPoolSize, cancellationToken);

        var connectionString = BuildConnectionString(
            databaseName,
            enablePooling: true,
            maxPoolSize: _settings.MaxPoolSize);

        return new TestDatabaseInstance(databaseName, connectionString);
    }

    public async Task DropDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
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

    public string BuildConnectionString(
        string databaseName,
        bool enablePooling = true,
        int? maxPoolSize = null)
    {
        if (_mappedPort == 0 || string.IsNullOrWhiteSpace(_host))
        {
            throw new InvalidOperationException("The PostgreSQL container has not been started yet.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = _host,
            Port = _mappedPort,
            Username = UserName,
            Password = Password,
            Database = databaseName,
            Pooling = enablePooling,
            IncludeErrorDetail = true
        };

        if (enablePooling && maxPoolSize.HasValue)
        {
            builder.MaxPoolSize = maxPoolSize.Value;
        }

        return builder.ConnectionString;
    }

    private async Task EnsureTemplateDatabaseAsync(CancellationToken cancellationToken)
    {
        EnsureAdminConnectionString();

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var existsCommand = connection.CreateCommand())
        {
            existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @databaseName";
            existsCommand.Parameters.AddWithValue("databaseName", _settings.TemplateDatabase);

            var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;
            if (exists)
            {
                return;
            }
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{EscapeIdentifier(_settings.TemplateDatabase)}\"";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string GenerateDatabaseName(Guid testId)
    {
        var testFragment = testId.ToString("N")[..8];
        var suffix = Guid.NewGuid().ToString("N")[..16];
        return $"test_{testFragment}_{suffix}".ToLowerInvariant();
    }

    private static string EscapeIdentifier(string identifier) => identifier.Replace("\"", "\"\"", StringComparison.Ordinal);

    private static async Task SetDatabaseConnectionLimitAsync(
        NpgsqlConnection adminConnection,
        string databaseName,
        int maxPoolSize,
        CancellationToken cancellationToken)
    {
        await using var connectionLimitCommand = adminConnection.CreateCommand();
        connectionLimitCommand.CommandText =
            $"ALTER DATABASE \"{EscapeIdentifier(databaseName)}\" WITH CONNECTION LIMIT {maxPoolSize}";
        await connectionLimitCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task TerminateActiveSessionsAsync(
        NpgsqlConnection adminConnection,
        string databaseName,
        CancellationToken cancellationToken)
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

    private void EnsureInitialized()
    {
        if (!_initialized || _adminConnectionString is null)
        {
            throw new InvalidOperationException("The PostgreSQL template database manager has not been initialized.");
        }
    }

    private void EnsureAdminConnectionString()
    {
        if (_adminConnectionString is null)
        {
            throw new InvalidOperationException("The PostgreSQL admin connection string is not available.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        _initialized = false;
        _adminConnectionString = null;
        _mappedPort = 0;
        _host = null;
    }
}
