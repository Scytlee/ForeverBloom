using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Errors;

public static class ApplicationErrors
{
    /// <summary>
    /// Error indicating a concurrency conflict occurred while handling the use case.
    /// </summary>
    public sealed record ConcurrencyConflict : ApplicationError
    {
        public override string Code => "Application.ConcurrencyConflict";
        public override string Message => "Concurrency conflict occurred while handling the use case. Please reload and retry.";
    }

    /// <summary>
    /// Error indicating an invalid row version value.
    /// </summary>
    public sealed record RowVersionInvalid([AttemptedValue] uint Value) : ApplicationError
    {
        public override string Code => "Application.RowVersionInvalid";
        public override string Message => $"The row version must be greater than 0, but was {Value}";
    }

    /// <summary>
    /// Error indicating a required field was not provided.
    /// </summary>
    public sealed record RequiredFieldMissing([AttemptedValue] string FieldName) : ApplicationError
    {
        public override string Code => "Application.RequiredFieldMissing";
        public override string Message => $"The field '{FieldName}' is required but was not provided";
    }
}
