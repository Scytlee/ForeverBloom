using ForeverBloom.Testing.Integration.Database;
using ForeverBloom.Testing.Integration.Time;

namespace ForeverBloom.Testing.Integration.Infrastructure;

/// <summary>
/// Provides a shared integration environment that owns the template database lifecycle,
/// per-test database creation, concurrency gating, and deterministic time services.
/// </summary>
internal sealed class IntegrationTestEnvironment : IAsyncDisposable
{
    private readonly Guid _environmentId = Guid.NewGuid();
    private readonly Func<string, CancellationToken, Task> _migrateTemplateDatabaseAsync;

    private TestDatabaseInstance? _databaseInstance;
    private Dictionary<string, string?>? _configurationValues;
    private bool _gateAcquired;
    private bool _disposed;

    private IntegrationTestEnvironment(
        Func<string, CancellationToken, Task> migrateTemplateDatabaseAsync,
        DateTimeOffset initialTime)
    {
        _migrateTemplateDatabaseAsync = migrateTemplateDatabaseAsync
            ?? throw new ArgumentNullException(nameof(migrateTemplateDatabaseAsync));
        TimeProvider = new TestTimeProvider(initialTime);
    }

    public static async Task<IntegrationTestEnvironment> CreateAsync(
        Func<string, CancellationToken, Task> migrateTemplateDatabaseAsync,
        DateTimeOffset initialTime,
        CancellationToken cancellationToken = default)
    {
        var environment = new IntegrationTestEnvironment(migrateTemplateDatabaseAsync, initialTime);
        await environment.InitializeAsync(cancellationToken);
        return environment;
    }

    public PostgresTemplateDatabaseManager TemplateManager => PostgresTemplateDatabaseLifetime.Manager;

    public TestTimeProvider TimeProvider { get; }

    public string ConnectionString =>
        _databaseInstance?.ConnectionString
        ?? throw new InvalidOperationException("The integration environment has not been initialized.");

    public IReadOnlyDictionary<string, string?> ConfigurationValues =>
        _configurationValues ?? throw new InvalidOperationException("Configuration values are not available.");

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await PostgresTemplateDatabaseLifetime.EnsureInitializedAsync(
            _migrateTemplateDatabaseAsync,
            cancellationToken);

        await TestInfrastructureConcurrencyGate.WaitAsync(cancellationToken);
        _gateAcquired = true;

        try
        {
            _databaseInstance = await TemplateManager.CreateDatabaseAsync(_environmentId, cancellationToken);
            _configurationValues = BuildConfigurationValues(_databaseInstance.ConnectionString);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_databaseInstance is not null)
        {
            await TemplateManager.DropDatabaseAsync(_databaseInstance.Name, CancellationToken.None);
            _databaseInstance = null;
        }

        if (_gateAcquired)
        {
            TestInfrastructureConcurrencyGate.Release();
            _gateAcquired = false;
        }
    }

    private static Dictionary<string, string?> BuildConfigurationValues(string connectionString)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:Postgres"] = connectionString
        };
    }
}
