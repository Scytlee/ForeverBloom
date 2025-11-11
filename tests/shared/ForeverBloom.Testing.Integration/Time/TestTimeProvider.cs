using ForeverBloom.Application.Abstractions.Time;

namespace ForeverBloom.Testing.Integration.Time;

/// <summary>
/// Minimal time provider for integration tests; always returns the current UTC time.
/// </summary>
internal sealed class TestTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
