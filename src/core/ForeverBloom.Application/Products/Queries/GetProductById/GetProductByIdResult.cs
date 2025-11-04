namespace ForeverBloom.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdResult
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? SeoTitle { get; set; }
    public string? FullDescription { get; set; }
    public string? MetaDescription { get; set; }
    public string Slug { get; set; } = null!;
    public decimal? Price { get; set; }
    public long CategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
    public int PublishStatusCode { get; set; }
    public int AvailabilityStatusCode { get; set; }
    public IReadOnlyList<GetProductByIdImageItem> Images { get; set; } = null!;

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public uint RowVersion { get; set; }
}

public sealed class GetProductByIdImageItem
{
    public long Id { get; set; }
    public string ImagePath { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
    public string? AltText { get; set; }
}
