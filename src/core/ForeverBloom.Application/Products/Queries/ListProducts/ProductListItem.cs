namespace ForeverBloom.Application.Products.Queries.ListProducts;

public sealed class ProductListItem
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public decimal? Price { get; set; }
    public string? MetaDescription { get; set; }
    public bool IsFeatured { get; set; }
    public int PublishStatus { get; set; }
    public int Availability { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int CategoryPublishStatus { get; set; }
    public string? ImageSource { get; set; }
    public string? ImageAltText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
