using ForeverBloom.Application.Abstractions.Time;

namespace ForeverBloom.Infrastructure.Time;

/// <summary>
/// System implementation of ITimeProvider that returns the actual current time.
/// </summary>
internal sealed class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
