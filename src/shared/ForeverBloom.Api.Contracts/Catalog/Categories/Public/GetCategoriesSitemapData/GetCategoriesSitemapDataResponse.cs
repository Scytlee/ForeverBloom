namespace ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoriesSitemapData;

public sealed record GetCategoriesSitemapDataResponse
{
    public List<CategorySitemapDataItem> Items { get; init; } = new();
}

public sealed record CategorySitemapDataItem
{
    public string Slug { get; init; } = null!;
    public DateOnly UpdatedOn { get; init; }
}
