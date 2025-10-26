using ForeverBloom.Domain.Shared.Models;

namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;

public sealed record CreateProductRequest
{
    public string Name { get; init; } = null!;
    public string? SeoTitle { get; init; }
    public string? FullDescription { get; init; }
    public string? MetaDescription { get; init; }
    public string Slug { get; init; } = null!;
    public decimal? Price { get; init; }
    public int DisplayOrder { get; init; } = 0;
    public bool IsFeatured { get; init; } = false;
    public PublishStatus PublishStatus { get; init; } = PublishStatus.Draft;
    public ProductAvailabilityStatus Availability { get; init; } = ProductAvailabilityStatus.Available;
    public int CategoryId { get; init; }
}
