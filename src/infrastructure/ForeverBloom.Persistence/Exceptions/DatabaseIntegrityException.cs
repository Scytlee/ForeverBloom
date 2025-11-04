using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Persistence.Exceptions;

/// <summary>
/// Exception thrown when data retrieved from the database violates domain invariants.
/// This indicates a critical data integrity issue that requires immediate investigation.
/// </summary>
public class DatabaseIntegrityException : Exception
{
    /// <summary>
    /// The entity type containing the invalid data.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// The invalid value retrieved from the database.
    /// </summary>
    public object? InvalidValue { get; }

    /// <summary>
    /// Detailed validation errors explaining why the data is invalid.
    /// </summary>
    public string ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of DatabaseIntegrityException.
    /// </summary>
    /// <param name="entityType">The entity type containing invalid data.</param>
    /// <param name="invalidValue">The invalid value from the database.</param>
    /// <param name="validationErrors">Formatted validation error messages.</param>
    public DatabaseIntegrityException(
        Type entityType,
        object? invalidValue,
        string validationErrors)
        : base(BuildMessage(entityType, invalidValue, validationErrors))
    {
        EntityType = entityType;
        InvalidValue = invalidValue;
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Initializes a new instance of DatabaseIntegrityException from a Result failure.
    /// </summary>
    /// <param name="entityType">The entity type containing invalid data.</param>
    /// <param name="invalidValue">The invalid value from the database.</param>
    /// <param name="result">The failed Result containing validation errors.</param>
    public DatabaseIntegrityException(
        Type entityType,
        object? invalidValue,
        IResult result)
        : this(entityType, invalidValue, FormatErrors(result))
    {
    }

    private static string BuildMessage(
        Type entityType,
        object? invalidValue,
        string validationErrors)
    {
        var valueDisplay = invalidValue switch
        {
            null => "<null>",
            string s when string.IsNullOrWhiteSpace(s) => "<empty or whitespace>",
            string s => $"\"{s}\"",
            _ => invalidValue.ToString() ?? "<null>"
        };

        return $"Database integrity violation occurred when creating a {entityType.Name} instance. "
               + $"Invalid value: {valueDisplay}. "
               + $"Validation errors: {validationErrors}. "
               + "This indicates corrupted or invalid data persisted in the database that violates domain invariants.";
    }

    private static string FormatErrors(IResult result)
    {
        if (result.Error is CompositeError composite)
        {
            return string.Join(", ", composite.Errors.Select(e => $"[{e.Code}] {e.Message}"));
        }

        return $"[{result.Error!.Code}] {result.Error.Message}";
    }
}
