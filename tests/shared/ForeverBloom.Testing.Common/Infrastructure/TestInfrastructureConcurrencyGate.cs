using ForeverBloom.Testing.Common.Config;

namespace ForeverBloom.Testing.Common.Infrastructure;

public static class TestInfrastructureConcurrencyGate
{
    private static readonly Lazy<SemaphoreSlim> LazyGate = new(CreateGate, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<TestInfrastructureSettings> LazySettings = new(TestInfrastructureSettingsLoader.Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public static TestInfrastructureSettings Settings => LazySettings.Value;

    public static SemaphoreSlim Gate => LazyGate.Value;

    public static Task WaitAsync(CancellationToken cancellationToken = default)
    {
        return Gate.WaitAsync(cancellationToken);
    }

    public static void Release()
    {
        Gate.Release();
    }

    private static SemaphoreSlim CreateGate()
    {
        var settings = Settings;
        return new SemaphoreSlim(settings.MaxConcurrentDatabases, settings.MaxConcurrentDatabases);
    }
}
