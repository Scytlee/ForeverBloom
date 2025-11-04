using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Sorting;

public static class SortingErrors
{
    public sealed record InvalidSortCriterionProperty(string AttemptedPropertyName, string[] AllowedProperties) : IError
    {
        public string Code => "Sorting.InvalidSortCriterionProperty";
        public string Message => $"Provided sort criterion column '{AttemptedPropertyName}' is invalid.";
    }

    public sealed record DuplicateSortCriterionProperty(string AttemptedPropertyName, int[] CriterionIndices) : IError
    {
        public string Code => "Sorting.DuplicateSortCriterionProperty";
        public string Message => $"Sort criterion column '{AttemptedPropertyName}' was provided multiple times at indices {string.Join(", ", CriterionIndices)}";
    }
}
