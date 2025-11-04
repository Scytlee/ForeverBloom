using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.WebApi.Endpoints.Products.CreateProduct;

/// <summary>
/// Request to create a new product.
/// </summary>
internal sealed record CreateProductRequest(
    string Name,
    string? SeoTitle,
    string? FullDescription,
    string? MetaDescription,
    string Slug,
    long CategoryId,
    decimal? Price,
    int DisplayOrder,
    bool IsFeatured,
    string AvailabilityStatus,
    IReadOnlyCollection<CreateProductRequestImage>? Images)
{
    internal CreateProductCommand ToCommand(ProductAvailabilityStatus availabilityStatus) => new(
        Name: Name,
        SeoTitle: SeoTitle,
        FullDescription: FullDescription,
        MetaDescription: MetaDescription,
        Slug: Slug,
        CategoryId: CategoryId,
        Price: Price,
        DisplayOrder: DisplayOrder,
        IsFeatured: IsFeatured,
        AvailabilityStatus: availabilityStatus,
        Images: Images?.Select(CreateProductRequestImage.ToCommandImage).ToArray());
}

/// <summary>
/// Represents an image supplied when creating a product via the API.
/// </summary>
internal sealed record CreateProductRequestImage(
    string Source,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder)
{
    internal static CreateProductCommandImage ToCommandImage(CreateProductRequestImage request) => new(
        Source: request.Source,
        AltText: request.AltText,
        IsPrimary: request.IsPrimary,
        DisplayOrder: request.DisplayOrder);
}
