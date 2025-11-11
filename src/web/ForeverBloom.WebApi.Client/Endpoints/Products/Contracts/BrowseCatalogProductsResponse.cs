namespace ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;

/// <summary>
/// Response containing paginated catalog products.
/// </summary>
public sealed record BrowseCatalogProductsResponse(
    IReadOnlyList<BrowseCatalogProductsResponseItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    /// <summary>
    /// Indicates whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Indicates whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Individual product item in the catalog browse results.
/// </summary>
public sealed record BrowseCatalogProductsResponseItem(
    long Id,
    string Name,
    string Slug,
    decimal? Price,
    string? MetaDescription,
    long CategoryId,
    string CategoryName,
    string? ImageSource,
    string? ImageAltText,
    ProductAvailabilityStatus AvailabilityStatus,
    bool IsFeatured);
