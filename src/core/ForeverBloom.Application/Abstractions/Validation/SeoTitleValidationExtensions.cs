using FluentValidation;
using ForeverBloom.Domain.Shared;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Validation extensions for SeoTitle value object.
/// </summary>
internal static class SeoTitleValidationExtensions
{
    /// <summary>
    /// Validates that a string is a valid SeoTitle by delegating to domain validation.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, string?> MustBeValidSeoTitle<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = SeoTitle.Create(value);
            if (result.IsSuccess)
            {
                return;
            }

            switch (result.Error)
            {
                case SeoTitleErrors.Empty e:
                    context.AddSeoTitleEmptyFailure(e, value);
                    break;
                case SeoTitleErrors.TooLong e:
                    context.AddSeoTitleTooLongFailure(e, value);
                    break;
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for an empty SEO title.
    /// </summary>
    private static void AddSeoTitleEmptyFailure<T>(this ValidationContext<T> context, SeoTitleErrors.Empty error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    /// <summary>
    /// Adds a validation failure for an SEO title that exceeds maximum length.
    /// </summary>
    private static void AddSeoTitleTooLongFailure<T>(this ValidationContext<T> context, SeoTitleErrors.TooLong error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        failure.CustomState = new { error.MaxLength };
        context.AddFailure(failure);
    }
}
