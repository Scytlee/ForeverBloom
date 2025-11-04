using FluentValidation;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// FluentValidation extensions for HtmlFragment value object validation.
/// </summary>
internal static class HtmlFragmentValidationExtensions
{
    /// <summary>
    /// Validates that a string is a valid HtmlFragment by delegating to domain validation.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, string?> MustBeValidHtmlFragment<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = HtmlFragment.Create(value!);
            if (result.IsSuccess)
            {
                return;
            }

            var composite = (CompositeError)result.Error;

            foreach (var error in composite.Errors)
            {
                switch (error)
                {
                    case HtmlFragmentErrors.Empty e:
                        context.AddHtmlFragmentEmptyFailure(e, value);
                        break;
                    case HtmlFragmentErrors.TooLong e:
                        context.AddHtmlFragmentTooLongFailure(e, value);
                        break;
                    case HtmlFragmentErrors.Malformed e:
                        context.AddHtmlFragmentMalformedFailure(e, value);
                        break;
                }
            }
        });
    }

    private static void AddHtmlFragmentEmptyFailure<T>(this ValidationContext<T> context, HtmlFragmentErrors.Empty error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    private static void AddHtmlFragmentTooLongFailure<T>(this ValidationContext<T> context, HtmlFragmentErrors.TooLong error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        failure.CustomState = new { MaxLength = HtmlFragment.MaxLength };
        context.AddFailure(failure);
    }

    private static void AddHtmlFragmentMalformedFailure<T>(this ValidationContext<T> context, HtmlFragmentErrors.Malformed error, string? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }
}
