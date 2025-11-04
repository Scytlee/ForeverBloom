namespace ForeverBloom.Application.Abstractions.Time;

/// <summary>
/// Provides access to the current time for application logic.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
