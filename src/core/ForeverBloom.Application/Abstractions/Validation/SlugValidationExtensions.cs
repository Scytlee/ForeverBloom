using FluentValidation;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Validation extensions for Slug value object.
/// </summary>
internal static class SlugValidationExtensions
{
    /// <summary>
    /// Validates that a string is a valid Slug by delegating to domain validation.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, string> MustBeValidSlug<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = Slug.Create(value);
            if (result.IsSuccess)
            {
                return;
            }

            var composite = (CompositeError)result.Error;

            foreach (var error in composite.Errors)
            {
                switch (error)
                {
                    case SlugErrors.Empty e:
                        context.AddSlugEmptyFailure(e, value);
                        break;
                    case SlugErrors.TooLong e:
                        context.AddSlugTooLongFailure(e, value);
                        break;
                    case SlugErrors.InvalidFormat e:
                        context.AddSlugInvalidFormatFailure(e, value);
                        break;
                }
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for an empty slug.
    /// </summary>
    private static void AddSlugEmptyFailure<T>(this ValidationContext<T> context, SlugErrors.Empty error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    /// <summary>
    /// Adds a validation failure for a slug that exceeds maximum length.
    /// </summary>
    private static void AddSlugTooLongFailure<T>(this ValidationContext<T> context, SlugErrors.TooLong error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        failure.CustomState = new { error.MaxLength };
        context.AddFailure(failure);
    }

    /// <summary>
    /// Adds a validation failure for a slug with invalid format.
    /// </summary>
    private static void AddSlugInvalidFormatFailure<T>(this ValidationContext<T> context, SlugErrors.InvalidFormat error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }
}
