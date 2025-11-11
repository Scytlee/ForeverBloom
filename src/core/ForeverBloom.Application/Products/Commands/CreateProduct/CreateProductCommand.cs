using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Application.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product.
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string? SeoTitle,
    string? FullDescription,
    string? MetaDescription,
    string Slug,
    long CategoryId,
    decimal? Price,
    bool IsFeatured,
    ProductAvailabilityStatus AvailabilityStatus,
    IReadOnlyCollection<CreateProductCommandImage>? Images
) : ICommand<CreateProductResult>;

/// <summary>
/// Represents a single image supplied when creating a product.
/// </summary>
public sealed record CreateProductCommandImage(
    string Source,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder);
