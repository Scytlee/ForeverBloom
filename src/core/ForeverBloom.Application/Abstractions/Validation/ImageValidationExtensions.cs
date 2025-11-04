using System.Linq.Expressions;
using FluentValidation;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Validation helpers for image-related input.
/// </summary>
internal static class ImageValidationExtensions
{
    internal static IRuleBuilderOptionsConditions<T, T> MustHaveValidImage<T>(
        this IRuleBuilder<T, T> ruleBuilder,
        Expression<Func<T, string>> imagePathSelector,
        Expression<Func<T, string?>> imageAltTextSelector)
    {
        var imagePathAccessor = imagePathSelector.Compile();
        var imageAltTextAccessor = imageAltTextSelector.Compile();

        var imagePathPropertyName = GetMemberName(imagePathSelector);
        var imageAltTextPropertyName = GetMemberName(imageAltTextSelector);

        return ruleBuilder.Custom((instance, context) =>
        {
            var imagePath = imagePathAccessor(instance);
            var imageAltText = imageAltTextAccessor(instance);

            MustHaveValidImageInternal(context, imagePath, imageAltText, imagePathPropertyName, imageAltTextPropertyName);
        });
    }

    internal static IRuleBuilderOptionsConditions<T, T> MustHaveValidImage<T>(
        this IRuleBuilder<T, T> ruleBuilder,
        Expression<Func<T, Optional<string>>> imagePathOptionalSelector,
        Expression<Func<T, Optional<string?>>> imageAltTextOptionalSelector)
    {
        var imagePathOptionalAccessor = imagePathOptionalSelector.Compile();
        var imageAltTextOptionalAccessor = imageAltTextOptionalSelector.Compile();

        var imagePathOptionalPropertyName = GetMemberName(imagePathOptionalSelector);
        var imageAltTextOptionalPropertyName = GetMemberName(imageAltTextOptionalSelector);

        return ruleBuilder.Custom((instance, context) =>
        {
            var imagePathOptional = imagePathOptionalAccessor(instance);
            var imageAltTextOptional = imageAltTextOptionalAccessor(instance);

            if (imagePathOptional.IsUnset || imageAltTextOptional.IsUnset)
            {
                return;
            }

            var imagePath = imagePathOptional.Value;
            var imageAltText = imageAltTextOptional.Value;

            MustHaveValidImageInternal(context, imagePath, imageAltText, imagePathOptionalPropertyName, imageAltTextOptionalPropertyName);
        });
    }

    private static void MustHaveValidImageInternal<T>(
        ValidationContext<T> context,
        string imagePath,
        string? imageAltText,
        string imagePathPropertyName,
        string imageAltTextPropertyName)
    {
        var result = Image.Create(imagePath, imageAltText);
        if (result.IsSuccess)
        {
            return;
        }

        var composite = (CompositeError)result.Error;
        foreach (var error in composite.Errors)
        {
            switch (error)
            {
                case UrlPathErrors.TooLong e:
                    context.AddImagePathTooLongFailure(e, imagePath, imagePathPropertyName);
                    break;
                case UrlPathErrors.InvalidFormat e:
                    context.AddImagePathInvalidFormatFailure(e, imagePath, imagePathPropertyName);
                    break;
                case ImageErrors.InvalidExtension e:
                    context.AddImagePathInvalidExtensionFailure(e, imagePath, imagePathPropertyName);
                    break;
                case ImageErrors.AltTextTooLong e:
                    context.AddImageAltTextTooLongFailure(e, imageAltText, imageAltTextPropertyName);
                    break;
            }
        }
    }

    internal static IRuleBuilderOptionsConditions<T, string?> MustBeValidAltText<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Custom((altText, context) =>
        {
            if (altText is not null && altText.Length > Image.AltTextMaxLength)
            {
                var error = new ImageErrors.AltTextTooLong(altText);
                context.AddImageAltTextTooLongFailure(error, altText, context.PropertyPath);
            }
        });
    }

    private static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> selector)
    {
        return selector.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression member } => member.Member.Name,
            _ => throw new InvalidOperationException("Selector must be a simple member access expression.")
        };
    }

    private static void AddImagePathTooLongFailure<T>(this ValidationContext<T> context, UrlPathErrors.TooLong error, string? attemptedValue, string propertyName)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue, propertyName);
        failure.CustomState = new { error.MaxLength };
        context.AddFailure(failure);
    }

    private static void AddImagePathInvalidFormatFailure<T>(this ValidationContext<T> context, UrlPathErrors.InvalidFormat error, string? attemptedValue, string propertyName)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue, propertyName);
        context.AddFailure(failure);
    }

    private static void AddImagePathInvalidExtensionFailure<T>(this ValidationContext<T> context, ImageErrors.InvalidExtension error, string? attemptedValue, string propertyName)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue, propertyName);
        failure.CustomState = new { error.AllowedExtensions };
        context.AddFailure(failure);
    }

    private static void AddImageAltTextTooLongFailure<T>(this ValidationContext<T> context, ImageErrors.AltTextTooLong error, string? attemptedValue, string propertyName)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue, propertyName);
        failure.CustomState = new { error.AltTextMaxLength };
        context.AddFailure(failure);
    }
}
