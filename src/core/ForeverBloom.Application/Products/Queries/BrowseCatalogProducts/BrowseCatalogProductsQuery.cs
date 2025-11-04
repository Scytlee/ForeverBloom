using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;

namespace ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;

public sealed record BrowseCatalogProductsQuery : PagedResultQuery<BrowseCatalogProductsResultItem>
{
    public static readonly HashSet<string> AllowedSortProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "price"
    };

    public SortCriterion[]? SortBy { get; init; }
    public long? CategoryId { get; init; }
    public bool? Featured { get; init; }
}
