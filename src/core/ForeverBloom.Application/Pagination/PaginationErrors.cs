using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Pagination;

public static class PaginationErrors
{
    public sealed record InvalidPageNumber(int AttemptedPageNumber) : IError
    {
        public string Code => "Pagination.InvalidPageNumber";
        public string Message => $"Provided page number '{AttemptedPageNumber}' is invalid.";
    }

    public sealed record InvalidPageSize(int AttemptedPageSize) : IError
    {
        public string Code => "Pagination.InvalidPageSize";
        public string Message => $"Provided page size '{AttemptedPageSize}' is invalid or out of range.";
        public int MinimumPageSize => PaginationConstants.MinimumPageSize;
        public int MaximumPageSize => PaginationConstants.MaximumPageSize;
    }
}
