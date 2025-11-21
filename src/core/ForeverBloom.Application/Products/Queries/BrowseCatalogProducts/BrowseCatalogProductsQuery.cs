using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;
using ForeverBloom.SharedKernel.Result;

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

    private BrowseCatalogProductsQuery() { }

    public int PageNumber { get; private init; } = PaginationConstants.DefaultPageNumber;
    public int PageSize { get; private init; } = PaginationConstants.DefaultPageSize;
    public SortStrategy SortStrategy { get; private init; } = null!;
    public long? CategoryId { get; private init; }
    public bool? Featured { get; private init; }

    public static Result<BrowseCatalogProductsQuery> Create(
        int pageNumber = PaginationConstants.DefaultPageNumber,
        int pageSize = PaginationConstants.DefaultPageSize,
        string sortStrategy = "relevance",
        long? categoryId = null,
        bool? featured = null)
    {
        var errors = new List<IError>();

        // Validate PageNumber
        if (pageNumber <= 0)
        {
            errors.Add(new PaginationErrors.InvalidPageNumber(pageNumber));
        }

        // Validate PageSize
        if (pageSize is < PaginationConstants.MinimumPageSize or > PaginationConstants.MaximumPageSize)
        {
            errors.Add(new PaginationErrors.InvalidPageSize(pageSize));
        }

        // Normalize and validate SortStrategy
        var normalizedSortStrategy = string.IsNullOrWhiteSpace(sortStrategy) ? "relevance" : sortStrategy;
        if (!AllowedSortStrategies.Contains(normalizedSortStrategy))
        {
            errors.Add(new SortingErrors.InvalidSortStrategy(
                normalizedSortStrategy,
                AllowedSortStrategies.ToArray()));
        }

        return Result<BrowseCatalogProductsQuery>.FromValidation(
            errors,
            () => new BrowseCatalogProductsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortStrategy = new SortStrategy(normalizedSortStrategy),
                CategoryId = categoryId,
                Featured = featured
            });
    }
}
