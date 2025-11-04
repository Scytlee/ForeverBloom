using FluentValidation;
using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Validation extensions for ProductName value object.
/// </summary>
internal static class ProductNameValidationExtensions
{
    /// <summary>
    /// Validates that a string is a valid ProductName by delegating to domain validation.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, string> MustBeValidProductName<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = ProductName.Create(value);
            if (result.IsSuccess)
            {
                return;
            }

            switch (result.Error)
            {
                case ProductNameErrors.Required e:
                    context.AddNameRequiredFailure(e, value);
                    break;
                case ProductNameErrors.TooLong e:
                    context.AddNameTooLongFailure(e, value);
                    break;
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for a required product name.
    /// </summary>
    private static void AddNameRequiredFailure<T>(this ValidationContext<T> context, ProductNameErrors.Required error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    /// <summary>
    /// Adds a validation failure for a product name that exceeds maximum length.
    /// </summary>
    private static void AddNameTooLongFailure<T>(this ValidationContext<T> context, ProductNameErrors.TooLong error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        failure.CustomState = new { error.MaxLength };
        context.AddFailure(failure);
    }
}
