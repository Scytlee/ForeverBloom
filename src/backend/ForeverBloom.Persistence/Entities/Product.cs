using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Entities.Interfaces;

namespace ForeverBloom.Persistence.Entities;

public sealed class Product : IAuditable, ISoftDeleteable
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string? SeoTitle { get; set; }
    public string? FullDescription { get; set; }
    public string? MetaDescription { get; set; }
    public string CurrentSlug { get; set; } = null!;
    public decimal? Price { get; set; } // Nullable for negotiable, made to order, or unknown pricing
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; } = false;
    public PublishStatus PublishStatus { get; set; } = PublishStatus.Draft;
    public ProductAvailabilityStatus Availability { get; set; } = ProductAvailabilityStatus.Available;

    // Foreign Key
    public int CategoryId { get; set; }

    // Navigation Properties
    public Category Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    // IAuditable
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }

    // ISoftDeleteable
    public DateTimeOffset? DeletedAt { get; set; }
}
