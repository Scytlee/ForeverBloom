using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;

namespace ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;

public sealed record BrowseCatalogProductsQuery : IQuery<BrowseCatalogProductsResult>
{
    public static readonly HashSet<string> AllowedSortStrategies = new(StringComparer.OrdinalIgnoreCase)
    {
        "relevance",
        "name_asc",
        "name_desc",
        "price_asc",
        "price_desc"
    };

    public int PageNumber { get; init; } = PaginationConstants.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationConstants.DefaultPageSize;
    public SortStrategy SortStrategy { get; init; } = null!;
    public long? CategoryId { get; init; }
    public bool? Featured { get; init; }
}
