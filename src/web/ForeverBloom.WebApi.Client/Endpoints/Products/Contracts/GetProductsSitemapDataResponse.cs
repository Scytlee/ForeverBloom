namespace ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;

/// <summary>
/// Response containing product sitemap data.
/// </summary>
public sealed record GetProductsSitemapDataResponse(
    IReadOnlyList<ProductSitemapDataItemResponse> Items);

/// <summary>
/// Individual product sitemap data item.
/// </summary>
public sealed record ProductSitemapDataItemResponse(
    string Slug,
    DateTimeOffset UpdatedAt);
