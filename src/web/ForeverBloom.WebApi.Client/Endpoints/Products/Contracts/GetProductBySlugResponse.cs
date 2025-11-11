namespace ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;

/// <summary>
/// Response containing full product details.
/// </summary>
public sealed record GetProductBySlugResponse(
    long Id,
    string Name,
    string? SeoTitle,
    string? FullDescription,
    string? MetaDescription,
    string Slug,
    decimal? Price,
    long CategoryId,
    string CategoryName,
    ProductAvailabilityStatus AvailabilityStatus,
    bool IsFeatured,
    IReadOnlyList<GetProductBySlugResponseImage> Images);

/// <summary>
/// Product image details.
/// </summary>
public sealed record GetProductBySlugResponseImage(
    string ImagePath,
    bool IsPrimary,
    int DisplayOrder,
    string? AltText);
