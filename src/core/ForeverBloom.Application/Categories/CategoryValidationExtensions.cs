using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories;

/// <summary>
/// FluentValidation helpers for category-specific rules.
/// </summary>
internal static class CategoryValidationExtensions
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
                context.AddCategoryIdInvalidFailure(new CategoryErrors.CategoryIdInvalid(value), value);
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for an invalid category ID.
    /// </summary>
    private static void AddCategoryIdInvalidFailure<T>(this ValidationContext<T> context, CategoryErrors.CategoryIdInvalid error, long attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    /// <summary>
    /// Validates that a parent category identifier (when provided) is positive.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, long?> MustBeValidParentCategoryId<T>(
        this IRuleBuilder<T, long?> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            if (value <= 0)
            {
                context.AddParentCategoryIdInvalidFailure(new Domain.Catalog.CategoryErrors.ParentCategoryIdInvalid(value.Value), value);
            }
        });
    }

    private static void AddParentCategoryIdInvalidFailure<T>(this ValidationContext<T> context, Domain.Catalog.CategoryErrors.ParentCategoryIdInvalid error, long? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    /// <summary>
    /// Validates that a category is not being set as its own parent.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, long?> MustNotBeOwnParent<T>(
        this IRuleBuilder<T, long?> ruleBuilder,
        Func<T, long> getCategoryId)
    {
        return ruleBuilder.Custom((newParentId, context) =>
        {
            if (newParentId.HasValue)
            {
                var categoryId = getCategoryId(context.InstanceToValidate);
                if (categoryId == newParentId.Value)
                {
                    context.AddCannotBeOwnParentFailure(
                        new Domain.Catalog.CategoryErrors.CannotBeOwnParent(categoryId),
                        newParentId);
                }
            }
        });
    }

    private static void AddCannotBeOwnParentFailure<T>(this ValidationContext<T> context, Domain.Catalog.CategoryErrors.CannotBeOwnParent error, long? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }
}
