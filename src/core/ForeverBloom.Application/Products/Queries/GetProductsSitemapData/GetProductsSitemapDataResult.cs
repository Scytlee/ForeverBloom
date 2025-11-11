namespace ForeverBloom.Application.Products.Queries.GetProductsSitemapData;

public sealed class GetProductsSitemapDataResult
{
    public List<ProductSitemapDataItem> Items { get; set; } = new();
}

public sealed class ProductSitemapDataItem
{
    public string Slug { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }
}
