namespace ForeverBloom.WebApi.Client.Endpoints.Categories.Contracts;

/// <summary>
/// Response containing hierarchical category tree.
/// </summary>
public sealed record BrowseCatalogCategoryTreeResponse(
    IReadOnlyList<BrowseCatalogCategoryTreeResponseItem> Categories);

/// <summary>
/// Individual category node in the tree with recursive children.
/// </summary>
public sealed record BrowseCatalogCategoryTreeResponseItem(
    long Id,
    string Name,
    string Slug,
    string? ImageSource,
    string? ImageAltText,
    IReadOnlyList<BrowseCatalogCategoryTreeResponseItem> Children);
