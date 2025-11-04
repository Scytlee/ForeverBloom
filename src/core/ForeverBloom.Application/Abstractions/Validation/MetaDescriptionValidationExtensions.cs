using FluentValidation;
using ForeverBloom.Domain.Shared;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Validation extensions for MetaDescription value object.
/// </summary>
internal static class MetaDescriptionValidationExtensions
{
    /// <summary>
    /// Validates that a string is a valid MetaDescription by delegating to domain validation.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, string?> MustBeValidMetaDescription<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = MetaDescription.Create(value);
            if (result.IsSuccess)
            {
                return;
            }

            switch (result.Error)
            {
                case MetaDescriptionErrors.Empty e:
                    context.AddMetaDescriptionEmptyFailure(e, value);
                    break;
                case MetaDescriptionErrors.TooLong e:
                    context.AddMetaDescriptionTooLongFailure(e, value);
                    break;
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for an empty meta description.
    /// </summary>
    private static void AddMetaDescriptionEmptyFailure<T>(this ValidationContext<T> context, MetaDescriptionErrors.Empty error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    /// <summary>
    /// Adds a validation failure for a meta description that exceeds maximum length.
    /// </summary>
    private static void AddMetaDescriptionTooLongFailure<T>(this ValidationContext<T> context, MetaDescriptionErrors.TooLong error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        failure.CustomState = new { error.MaxLength };
        context.AddFailure(failure);
    }
}
