using ForeverBloom.Application.Products.Commands.UpdateProductImages;

namespace ForeverBloom.WebApi.Endpoints.Products.UpdateProductImages;

internal sealed record UpdateProductImagesResponse(
    IReadOnlyList<UpdateProductImagesResponseImage> Images,
    uint RowVersion)
{
    internal static UpdateProductImagesResponse FromResult(UpdateProductImagesResult result)
    {
        var images = result.Images
            .Select(image => new UpdateProductImagesResponseImage(
                image.Id,
                image.Source,
                image.AltText,
                image.IsPrimary,
                image.DisplayOrder))
            .ToArray();

        return new UpdateProductImagesResponse(images, result.RowVersion);
    }
}

internal sealed record UpdateProductImagesResponseImage(
    long Id,
    string Source,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder);
