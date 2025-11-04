using FluentValidation;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Validation extensions that apply to base entity concepts.
/// </summary>
internal static class EntityValidationExtensions
{
    /// <summary>
    /// Ensures the row version is populated with a positive value.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, uint> MustBeValidRowVersion<T>(
        this IRuleBuilder<T, uint> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            if (value <= 0)
            {
                context.AddRowVersionInvalidFailure(new RowVersionInvalid(value), value);
            }
        });
    }

    private static void AddRowVersionInvalidFailure<T>(
        this ValidationContext<T> context,
        RowVersionInvalid error,
        uint attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    private sealed record RowVersionInvalid(uint AttemptedValue) : IError
    {
        public string Code => "Entity.RowVersionInvalid";
        public string Message => $"The row version '{AttemptedValue}' is invalid.";
    }
}
