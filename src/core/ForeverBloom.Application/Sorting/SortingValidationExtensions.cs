using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Sorting;

internal static class SortingValidationExtensions
{
    internal static IRuleBuilderOptionsConditions<T, SortCriterion> MustBeValidSortCriterion<T>(
        this IRuleBuilder<T, SortCriterion> ruleBuilder,
        HashSet<string> allowedSortProperties)
    {
        return ruleBuilder.Custom((criterion, context) =>
        {
            var allowedSortPropertiesArray = allowedSortProperties.ToArray();

            if (!allowedSortProperties.Contains(criterion.PropertyName))
            {
                context.AddInvalidSortCriterionPropertyFailure(
                    new SortingErrors.InvalidSortCriterionProperty(criterion.PropertyName, allowedSortPropertiesArray));
            }
        });
    }

    private static void AddInvalidSortCriterionPropertyFailure<T>(
        this ValidationContext<T> context,
        SortingErrors.InvalidSortCriterionProperty error)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, error.AttemptedPropertyName);
        failure.CustomState = new { error.AllowedProperties };
        context.AddFailure(failure);
    }

    internal static IRuleBuilderOptionsConditions<T, SortCriterion[]> MustHaveNoDuplicateProperties<T>(
        this IRuleBuilder<T, SortCriterion[]> ruleBuilder)
    {
        return ruleBuilder.Custom((criteria, context) =>
        {
            var duplicateProperties = criteria
                .Select((criterion, index) => (criterion.PropertyName, Index: index))
                .GroupBy(criterion => criterion.PropertyName)
                .Where(group => group.Count() > 1)
                .Select(group => (Name: group.Key, CriterionIndices: group.Select(x => x.Index).ToArray()))
                .ToArray();

            foreach (var property in duplicateProperties)
            {
                context.AddDuplicateSortCriterionPropertyFailure(
                    new SortingErrors.DuplicateSortCriterionProperty(property.Name, property.CriterionIndices));
            }
        });
    }

    private static void AddDuplicateSortCriterionPropertyFailure<T>(
        this ValidationContext<T> context,
        SortingErrors.DuplicateSortCriterionProperty error)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, error.AttemptedPropertyName);
        failure.CustomState = new { error.CriterionIndices };
        context.AddFailure(failure);
    }
}
