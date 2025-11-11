using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Sorting;

internal static class SortingValidationExtensions
{
    internal static IRuleBuilderOptionsConditions<T, SortStrategy> MustBeValidSortStrategy<T>(
        this IRuleBuilder<T, SortStrategy> ruleBuilder,
        HashSet<string> allowedSortStrategies)
    {
        return ruleBuilder.Custom((strategy, context) =>
        {
            var allowedSortStrategiesArray = allowedSortStrategies.ToArray();

            if (!allowedSortStrategies.Contains(strategy.Id))
            {
                context.AddInvalidSortStrategyFailure(
                    new SortingErrors.InvalidSortStrategy(strategy.Id, allowedSortStrategiesArray));
            }
        });
    }

    private static void AddInvalidSortStrategyFailure<T>(
        this ValidationContext<T> context,
        SortingErrors.InvalidSortStrategy error)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, error.AttemptedStrategyId);
        failure.CustomState = new { error.AllowedStrategies };
        context.AddFailure(failure);
    }

    internal static IRuleBuilderOptionsConditions<T, SortProperty> MustBeValidSortProperty<T>(
        this IRuleBuilder<T, SortProperty> ruleBuilder,
        HashSet<string> allowedSortProperties)
    {
        return ruleBuilder.Custom((property, context) =>
        {
            var allowedSortPropertiesArray = allowedSortProperties.ToArray();

            if (!allowedSortProperties.Contains(property.Name))
            {
                context.AddInvalidSortPropertyFailure(
                    new SortingErrors.InvalidSortProperty(property.Name, allowedSortPropertiesArray));
            }
        });
    }

    private static void AddInvalidSortPropertyFailure<T>(
        this ValidationContext<T> context,
        SortingErrors.InvalidSortProperty error)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, error.AttemptedPropertyName);
        failure.CustomState = new { error.AllowedProperties };
        context.AddFailure(failure);
    }

    internal static IRuleBuilderOptionsConditions<T, SortProperty[]> MustHaveNoDuplicateProperties<T>(
        this IRuleBuilder<T, SortProperty[]> ruleBuilder)
    {
        return ruleBuilder.Custom((properties, context) =>
        {
            var duplicateProperties = properties
                .Select((property, index) => (property.Name, Index: index))
                .GroupBy(property => property.Name)
                .Where(group => group.Count() > 1)
                .Select(group => (Name: group.Key, Indices: group.Select(x => x.Index).ToArray()))
                .ToArray();

            foreach (var property in duplicateProperties)
            {
                context.AddDuplicateSortPropertyFailure(
                    new SortingErrors.DuplicateSortProperty(property.Name, property.Indices));
            }
        });
    }

    private static void AddDuplicateSortPropertyFailure<T>(
        this ValidationContext<T> context,
        SortingErrors.DuplicateSortProperty error)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, error.AttemptedPropertyName);
        failure.CustomState = new { error.PropertyIndices };
        context.AddFailure(failure);
    }
}
