namespace ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;

public sealed class BrowseCatalogCategoryTreeResult
{
    public IReadOnlyList<BrowseCatalogCategoryTreeResultItem> Categories { get; set; } = [];
}

public sealed class BrowseCatalogCategoryTreeResultItem
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ImageSource { get; set; }
    public string? ImageAltText { get; set; }
    public IReadOnlyList<BrowseCatalogCategoryTreeResultItem> Children { get; set; } = [];
}
