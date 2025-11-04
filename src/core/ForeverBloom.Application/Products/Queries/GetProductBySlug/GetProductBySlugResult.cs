namespace ForeverBloom.Application.Products.Queries.GetProductBySlug;

public sealed class GetProductBySlugResult
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? SeoTitle { get; set; }
    public string? FullDescription { get; set; }
    public string? MetaDescription { get; set; }
    public string Slug { get; set; } = null!;
    public decimal? Price { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int AvailabilityStatusCode { get; set; }
    public bool IsFeatured { get; set; }
    public IReadOnlyList<GetProductBySlugImageItem> Images { get; set; } = null!;
}

public sealed class GetProductBySlugImageItem
{
    public string ImagePath { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
    public string? AltText { get; set; }
}
