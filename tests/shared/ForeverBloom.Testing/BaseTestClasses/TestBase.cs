using Xunit;

namespace ForeverBloom.Testing.BaseTestClasses;

/// <summary>
/// Base class for all tests providing common test utilities.
/// </summary>
public abstract class TestBase
{
    /// <summary>
    /// Gets a unique test token (6-character alphanumeric string) for this test instance.
    /// </summary>
    protected string TestToken { get; } = Guid.NewGuid().ToString("N")[..6];

    /// <summary>
    /// Gets the cancellation token for the current test context.
    /// </summary>
    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
}
