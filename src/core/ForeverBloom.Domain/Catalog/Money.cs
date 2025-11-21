using ForeverBloom.Domain.Abstractions.Errors;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

/// <summary>
/// Represents a monetary value with business validation rules.
/// </summary>
public sealed record Money
{
    public decimal Value { get; }

    public const int RequiredDecimalPlaces = 2;

    private Money(decimal value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Money instance with domain validation.
    /// </summary>
    /// <param name="value">The monetary value to validate and create.</param>
    /// <returns>A Result containing either a valid Money or validation errors.</returns>
    public static Result<Money> Create(decimal value)
    {
        var errors = new List<IError>();

        if (value < 0)
        {
            errors.Add(new MoneyErrors.Negative(value));
        }

        if (value != decimal.Round(value, RequiredDecimalPlaces))
        {
            errors.Add(new MoneyErrors.InvalidPrecision(value));
        }

        return Result<Money>.FromValidation(errors, () => new Money(value));
    }

    /// <summary>
    /// Implicit conversion to decimal for convenience.
    /// </summary>
    public static implicit operator decimal(Money money) => money.Value;
}

public static class MoneyErrors
{
    public sealed record Negative([AttemptedValue] decimal Value) : DomainError
    {
        public override string Code => "Money.Negative";
        public override string Message => $"Money value cannot be negative, but was {Value}";
    }

    public sealed record InvalidPrecision([AttemptedValue] decimal Value) : DomainError
    {
        public override string Code => "Money.InvalidPrecision";
        public override string Message => $"Money value must be representable with {RequiredDecimalPlaces} decimal places without losing precision, but was {Value}";
        public static int RequiredDecimalPlaces => Money.RequiredDecimalPlaces;
    }
}
