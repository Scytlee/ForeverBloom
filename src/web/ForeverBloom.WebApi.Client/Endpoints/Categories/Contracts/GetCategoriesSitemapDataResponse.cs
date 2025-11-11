namespace ForeverBloom.WebApi.Client.Endpoints.Categories.Contracts;

/// <summary>
/// Response containing category sitemap data.
/// </summary>
public sealed record GetCategoriesSitemapDataResponse(
    IReadOnlyList<CategorySitemapDataItemResponse> Items);

/// <summary>
/// Individual category sitemap data item.
/// </summary>
public sealed record CategorySitemapDataItemResponse(
    string Slug,
    DateTimeOffset UpdatedAt);
