using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductResult(
    string Name,
    string? SeoTitle,
    string? FullDescription,
    string? MetaDescription,
    long CategoryId,
    decimal? Price,
    int DisplayOrder,
    bool IsFeatured,
    ProductAvailabilityStatus Availability,
    PublishStatus PublishStatus,
    DateTimeOffset UpdatedAt,
    uint RowVersion);
