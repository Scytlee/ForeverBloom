namespace ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductsSitemapData;

public sealed record GetProductsSitemapDataResponse
{
    public List<ProductSitemapDataItem> Items { get; init; } = new();
}

public sealed record ProductSitemapDataItem
{
    public string Slug { get; init; } = null!;
    public DateOnly UpdatedOn { get; init; }
}
