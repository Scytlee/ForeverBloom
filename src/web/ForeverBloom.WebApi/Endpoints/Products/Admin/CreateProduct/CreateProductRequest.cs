using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.CreateProduct;

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
    bool IsFeatured,
    string AvailabilityStatus,
    IReadOnlyCollection<CreateProductRequestImage>? Images)
{
    internal Result<CreateProductCommand> ToCommand() =>
        CreateProductCommand.Create(
            name: Name,
            slug: Slug,
            categoryId: CategoryId,
            availabilityStatus: AvailabilityStatus,
            isFeatured: IsFeatured,
            seoTitle: SeoTitle,
            fullDescription: FullDescription,
            metaDescription: MetaDescription,
            price: Price,
            images: Images?.Select(CreateProductRequestImage.ToCommandImage).ToArray());
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
