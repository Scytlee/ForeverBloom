using FluentValidation;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// FluentValidation extensions for Money value object validation.
/// </summary>
internal static class MoneyValidationExtensions
{
    /// <summary>
    /// Validates that a nullable decimal is a valid Money by delegating to domain validation.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, decimal?> MustBeValidMoney<T>(
        this IRuleBuilder<T, decimal?> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = Money.Create(value!.Value);
            if (result.IsSuccess)
            {
                return;
            }

            var composite = (CompositeError)result.Error;

            foreach (var error in composite.Errors)
            {
                switch (error)
                {
                    case MoneyErrors.NotPositive e:
                        context.AddMoneyNotPositiveFailure(e, value.Value);
                        break;
                    case MoneyErrors.InvalidPrecision e:
                        context.AddMoneyInvalidPrecisionFailure(e, value.Value);
                        break;
                }
            }
        });
    }

    private static void AddMoneyNotPositiveFailure<T>(this ValidationContext<T> context, MoneyErrors.NotPositive error, decimal attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    private static void AddMoneyInvalidPrecisionFailure<T>(this ValidationContext<T> context, MoneyErrors.InvalidPrecision error, decimal attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        failure.CustomState = new { Money.RequiredDecimalPlaces };
        context.AddFailure(failure);
    }
}
