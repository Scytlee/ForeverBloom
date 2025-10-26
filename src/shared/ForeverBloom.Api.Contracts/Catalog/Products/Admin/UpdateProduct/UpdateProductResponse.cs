using ForeverBloom.Domain.Shared.Models;

namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProduct;

public sealed record UpdateProductResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? SeoTitle { get; init; }
    public string? FullDescription { get; init; }
    public string? MetaDescription { get; init; }
    public string Slug { get; init; } = null!;
    public decimal? Price { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsFeatured { get; init; }
    public PublishStatus PublishStatus { get; init; }
    public ProductAvailabilityStatus Availability { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public string CategorySlug { get; init; } = null!;
    public IReadOnlyList<AdminProductImageItem> Images { get; init; } = [];

    // Audit fields
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
    public uint RowVersion { get; init; }
}

public sealed record AdminProductImageItem
{
    public string ImagePath { get; init; } = null!;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
    public string? AltText { get; init; }
}
