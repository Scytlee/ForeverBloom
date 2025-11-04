using ForeverBloom.Application.Products.Queries.GetProductBySlug;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.Endpoints.Products.GetProductBySlug;

/// <summary>
/// Response payload returned for the public GetProductBySlug endpoint.
/// </summary>
internal sealed record GetProductBySlugResponse(
    long Id,
    string Name,
    string? SeoTitle,
    string? FullDescription,
    string? MetaDescription,
    string Slug,
    decimal? Price,
    long CategoryId,
    string CategoryName,
    string AvailabilityStatus,
    bool IsFeatured,
    IReadOnlyList<GetProductBySlugResponseImage> Images)
{
    internal static GetProductBySlugResponse FromResult(GetProductBySlugResult result)
    {
        var images = result.Images
            .Select(image => new GetProductBySlugResponseImage(
                ImagePath: image.ImagePath,
                IsPrimary: image.IsPrimary,
                DisplayOrder: image.DisplayOrder,
                AltText: image.AltText))
            .ToArray();

        return new GetProductBySlugResponse(
            Id: result.Id,
            Name: result.Name,
            SeoTitle: result.SeoTitle,
            FullDescription: result.FullDescription,
            MetaDescription: result.MetaDescription,
            Slug: result.Slug,
            Price: result.Price,
            CategoryId: result.CategoryId,
            CategoryName: result.CategoryName,
            AvailabilityStatus: AvailabilityStatusMapper.ToString(result.AvailabilityStatusCode),
            IsFeatured: result.IsFeatured,
            Images: images);
    }
}

internal sealed record GetProductBySlugResponseImage(
    string ImagePath,
    bool IsPrimary,
    int DisplayOrder,
    string? AltText);
