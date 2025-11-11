using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;

namespace ForeverBloom.Application.Products.Queries.ListProducts;

public sealed record ListProductsQuery : IQuery<ListProductsResult>
{
    public static readonly HashSet<string> AllowedSortProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "price",
        "created_at",
        "updated_at",
        "category_name"
    };

    public int PageNumber { get; init; } = PaginationConstants.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationConstants.DefaultPageSize;
    public SortProperty[]? SortBy { get; init; }
    public string? SearchTerm { get; init; }
    public long? CategoryId { get; init; }
    public bool? IncludeSubcategories { get; init; }
}
