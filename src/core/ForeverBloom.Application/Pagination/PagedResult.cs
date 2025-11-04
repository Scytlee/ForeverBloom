namespace ForeverBloom.Application.Pagination;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="TItem">The type of items in the result.</typeparam>
public abstract class PagedResult<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
