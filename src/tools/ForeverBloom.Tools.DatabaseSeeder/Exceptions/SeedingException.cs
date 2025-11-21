using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Tools.DatabaseSeeder.Exceptions;

/// <summary>
/// Exception thrown when seeding operations fail.
/// </summary>
public class SeedingException : Exception
{
    /// <summary>
    /// Gets the name of the seed item that failed.
    /// </summary>
    public string SeedItemName { get; }

    /// <summary>
    /// Gets the domain error that caused the failure, if available.
    /// </summary>
    public IError? DomainError { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedingException"/> class.
    /// </summary>
    /// <param name="seedItemName">The name of the seed item that failed.</param>
    /// <param name="error">The domain error that caused the failure.</param>
    public SeedingException(string seedItemName, IError error)
        : base($"Failed to seed '{seedItemName}': {error.Message}")
    {
        SeedItemName = seedItemName;
        DomainError = error;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedingException"/> class.
    /// </summary>
    /// <param name="seedItemName">The name of the seed item that failed.</param>
    /// <param name="message">The error message.</param>
    public SeedingException(string seedItemName, string message)
        : base($"Failed to seed '{seedItemName}': {message}")
    {
        SeedItemName = seedItemName;
        DomainError = null;
    }
}
