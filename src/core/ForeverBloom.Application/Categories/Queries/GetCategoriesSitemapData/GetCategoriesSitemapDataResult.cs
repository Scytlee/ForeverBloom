namespace ForeverBloom.Application.Categories.Queries.GetCategoriesSitemapData;

public sealed class GetCategoriesSitemapDataResult
{
    public List<CategorySitemapDataItem> Items { get; set; } = new();
}

public sealed class CategorySitemapDataItem
{
    public string Slug { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }
}
