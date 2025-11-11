using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;

internal static class BrowseCatalogCategoryTreeValidationExtensions
{
    internal static IRuleBuilderOptionsConditions<T, int?> MustBeValidDepth<T>(
        this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            if (value is not null && value < 0)
            {
                context.AddDepthOutOfRangeFailure(
                    new BrowseCatalogCategoryTreeErrors.DepthOutOfRange(value.Value),
                    value);
            }
        });
    }

    private static void AddDepthOutOfRangeFailure<T>(
        this ValidationContext<T> context,
        BrowseCatalogCategoryTreeErrors.DepthOutOfRange error,
        int? attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }
}
