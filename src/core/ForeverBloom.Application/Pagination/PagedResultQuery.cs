using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Pagination;

public abstract record PagedResultQuery<TItem> : IQuery<PagedResult<TItem>>
{
    public int PageNumber { get; init; } = PaginationConstants.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationConstants.DefaultPageSize;
}
