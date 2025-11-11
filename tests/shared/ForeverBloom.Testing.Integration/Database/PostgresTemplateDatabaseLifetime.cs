using ForeverBloom.Testing.Integration.Infrastructure;

namespace ForeverBloom.Testing.Integration.Database;

internal static class PostgresTemplateDatabaseLifetime
{
    private static readonly Lazy<PostgresTemplateDatabaseManager> LazyManager =
        new(() => new PostgresTemplateDatabaseManager(TestInfrastructureConcurrencyGate.Settings));

    private static readonly SemaphoreSlim InitializationLock = new(1, 1);
    private static bool _initialized;
    private static bool _disposed;

    static PostgresTemplateDatabaseLifetime()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeManager();
        AppDomain.CurrentDomain.DomainUnload += (_, _) => DisposeManager();
    }

    public static PostgresTemplateDatabaseManager Manager => LazyManager.Value;

    public static async Task EnsureInitializedAsync(
        Func<string, CancellationToken, Task> migrateTemplateDatabaseAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(migrateTemplateDatabaseAsync);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PostgresTemplateDatabaseLifetime));
        }

        if (_initialized)
        {
            return;
        }

        await InitializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await Manager.InitializeAsync(migrateTemplateDatabaseAsync, cancellationToken);
            _initialized = true;
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    private static void DisposeManager()
    {
        if (_disposed || !LazyManager.IsValueCreated)
        {
            _disposed = true;
            return;
        }

        try
        {
            Manager.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch
        {
            // Swallow exceptions during process teardown.
        }
        finally
        {
            _initialized = false;
            _disposed = true;
        }
    }
}
