namespace ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;

/// <summary>
/// Request parameters for browsing catalog products.
/// </summary>
public sealed record BrowseCatalogProductsRequest
{
    /// <summary>
    /// The page number (1-based).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// The page size (number of items per page).
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// The sort strategy (e.g., "relevance", "price-asc", "price-desc", "name").
    /// </summary>
    public string? Sort { get; init; }

    /// <summary>
    /// Optional category ID to filter by.
    /// </summary>
    public long? CategoryId { get; init; }

    /// <summary>
    /// Optional flag to filter featured products.
    /// </summary>
    public bool? Featured { get; init; }
}
