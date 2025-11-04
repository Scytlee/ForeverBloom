namespace ForeverBloom.Application.Products.Commands.UpdateProductImages;

public sealed record UpdateProductImagesResult(
    IReadOnlyList<UpdateProductImagesResultImage> Images,
    uint RowVersion);

public sealed record UpdateProductImagesResultImage(
    long Id,
    string Source,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder);
