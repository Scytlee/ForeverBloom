using ForeverBloom.Domain.Shared.Models;

namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;

public sealed record CreateProductResponse
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

    // Audit fields
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
    public uint RowVersion { get; init; }
}
