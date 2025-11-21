namespace ForeverBloom.WebApi.Client.Endpoints.Categories.Contracts;

/// <summary>
/// Request parameters for browsing catalog category tree.
/// </summary>
public sealed record BrowseCatalogCategoryTreeRequest
{
    /// <summary>
    /// Optional root category ID to start the tree from.
    /// </summary>
    public long? RootCategoryId { get; init; }

    /// <summary>
    /// Optional maximum levels of the tree to retrieve.
    /// </summary>
    public int? Levels { get; init; }
}
