namespace ForeverBloom.ApiClient.Contracts;

/// <summary>
/// Represents a validation error, which is a specific type of Error
/// containing a collection of validation failures.
/// </summary>
public sealed class ValidationError : Error
{
    /// <summary>
    /// Gets the collection of validation failures. The key is the property name
    /// and the value is an array of error messages for that property.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Failures { get; }

    public ValidationError(IDictionary<string, string[]> failures)
      : base("General.Validation", "One or more validation errors occurred.")
    {
        Failures = failures.AsReadOnly();
    }
}
