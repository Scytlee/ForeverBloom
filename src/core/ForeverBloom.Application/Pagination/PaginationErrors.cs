using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Pagination;

public static class PaginationErrors
{
    public sealed record InvalidPageNumber([AttemptedValue] int PageNumber) : ApplicationError
    {
        public override string Code => "Pagination.InvalidPageNumber";
        public override string Message => $"Page number {PageNumber} is invalid";
    }

    public sealed record InvalidPageSize([AttemptedValue] int PageSize) : ApplicationError
    {
        public override string Code => "Pagination.InvalidPageSize";
        public override string Message => $"Page size must be between {MinimumPageSize} and {MaximumPageSize}, but was {PageSize}";
        public static int MinimumPageSize => PaginationConstants.MinimumPageSize;
        public static int MaximumPageSize => PaginationConstants.MaximumPageSize;
    }
}
