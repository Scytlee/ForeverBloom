namespace ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;

public sealed class BrowseCatalogProductsResultItem
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public decimal? Price { get; set; }
    public string? MetaDescription { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? ImageSource { get; set; }
    public string? ImageAltText { get; set; }
    public int AvailabilityStatusCode { get; set; }
    public bool IsFeatured { get; set; }
}
