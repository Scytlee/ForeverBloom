using ForeverBloom.Domain.Shared.Models;

namespace ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductBySlug;

public sealed record GetProductBySlugResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? SeoTitle { get; init; }
    public string? FullDescription { get; init; }
    public string? MetaDescription { get; init; }
    public string Slug { get; init; } = null!;
    public decimal? Price { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public ProductAvailabilityStatus Availability { get; init; }
    public bool IsFeatured { get; init; }
    public IReadOnlyList<ProductImageItem> Images { get; init; } = null!;
}

public sealed record ProductImageItem
{
    public string ImagePath { get; init; } = null!;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
    public string? AltText { get; init; }
}
