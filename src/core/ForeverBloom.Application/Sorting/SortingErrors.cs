using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Sorting;

public static class SortingErrors
{
    public sealed record InvalidSortFormat([AttemptedValue] string Value) : ApplicationError
    {
        public override string Code => "Sorting.InvalidSortFormat";
        public override string Message => $"Each sort property must be in the format 'property:direction' (e.g., 'name:asc'), but received '{Value}'";
    }

    public sealed record InvalidSortDirection([AttemptedValue] string Direction) : ApplicationError
    {
        public override string Code => "Sorting.InvalidSortDirection";
        public override string Message => $"Sort direction must be either 'asc' or 'desc', but received '{Direction}'";
        public static readonly string[] ValidDirections = ["asc", "desc"];
    }

    public sealed record InvalidSortStrategy([AttemptedValue] string StrategyId, string[] AllowedStrategies) : ApplicationError
    {
        public override string Code => "Sorting.InvalidSortStrategy";
        public override string Message => $"Sort strategy '{StrategyId}' is invalid";
    }

    public sealed record InvalidSortProperty([AttemptedValue] string PropertyName, string[] AllowedProperties) : ApplicationError
    {
        public override string Code => "Sorting.InvalidSortProperty";
        public override string Message => $"Sort property '{PropertyName}' is invalid";
    }

    public sealed record DuplicateSortProperty([AttemptedValue] string PropertyName, int[] PropertyIndices) : ApplicationError
    {
        public override string Code => "Sorting.DuplicateSortProperty";
        public override string Message => $"Sort property '{PropertyName}' was provided multiple times at indices {string.Join(", ", PropertyIndices)}";
    }
}
