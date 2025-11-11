using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Sorting;

public static class SortingErrors
{
    public sealed record InvalidSortStrategy(string AttemptedStrategyId, string[] AllowedStrategies) : IError
    {
        public string Code => "Sorting.InvalidSortStrategy";
        public string Message => $"Provided sort strategy '{AttemptedStrategyId}' is invalid.";
    }

    public sealed record InvalidSortProperty(string AttemptedPropertyName, string[] AllowedProperties) : IError
    {
        public string Code => "Sorting.InvalidSortProperty";
        public string Message => $"Provided sort property '{AttemptedPropertyName}' is invalid.";
    }

    public sealed record DuplicateSortProperty(string AttemptedPropertyName, int[] PropertyIndices) : IError
    {
        public string Code => "Sorting.DuplicateSortProperty";
        public string Message => $"Sort property '{AttemptedPropertyName}' was provided multiple times at indices {string.Join(", ", PropertyIndices)}";
    }
}
