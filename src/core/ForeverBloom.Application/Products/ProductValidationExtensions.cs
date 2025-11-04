using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Products;

/// <summary>
/// Validation extensions for Product entity-level validations.
/// </summary>
internal static class ProductValidationExtensions
{
    /// <summary>
    /// Validates that a category ID is valid (greater than 0).
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, long> MustBeValidCategoryId<T>(
        this IRuleBuilder<T, long> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            if (value <= 0)
            {
                context.AddCategoryIdInvalidFailure(new Domain.Catalog.ProductErrors.CategoryIdInvalid(value), value);
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for an invalid category ID.
    /// </summary>
    private static void AddCategoryIdInvalidFailure<T>(this ValidationContext<T> context, Domain.Catalog.ProductErrors.CategoryIdInvalid error, long attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    /// <summary>
    /// Validates that a product ID is valid (greater than 0).
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, long> MustBeValidProductId<T>(
        this IRuleBuilder<T, long> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            if (value <= 0)
            {
                context.AddProductIdInvalidFailure(new ProductErrors.ProductIdInvalid(value), value);
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for an invalid product ID.
    /// </summary>
    private static void AddProductIdInvalidFailure<T>(this ValidationContext<T> context, ProductErrors.ProductIdInvalid error, long attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    /// <summary>
    /// Validates that an image ID is valid (greater than 0).
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, long> MustBeValidImageId<T>(
        this IRuleBuilder<T, long> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            if (value <= 0)
            {
                context.AddImageIdInvalidFailure(new ProductErrors.ImageIdInvalid(value), value);
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for an invalid image ID.
    /// </summary>
    private static void AddImageIdInvalidFailure<T>(this ValidationContext<T> context, ProductErrors.ImageIdInvalid error, long attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }
}
