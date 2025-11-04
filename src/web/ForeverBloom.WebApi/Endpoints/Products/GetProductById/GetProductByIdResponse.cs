using ForeverBloom.Application.Products.Queries.GetProductById;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.Endpoints.Products.GetProductById;

/// <summary>
/// Response payload returned for the admin GetProductById endpoint.
/// </summary>
internal sealed record GetProductByIdResponse(
    long Id,
    string Name,
    string? SeoTitle,
    string? FullDescription,
    string? MetaDescription,
    string Slug,
    decimal? Price,
    long CategoryId,
    int DisplayOrder,
    bool IsFeatured,
    string PublishStatus,
    string AvailabilityStatus,
    IReadOnlyList<GetProductByIdResponseImage> Images,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt,
    uint RowVersion)
{
    internal static GetProductByIdResponse FromResult(GetProductByIdResult result)
    {
        var images = result.Images
            .Select(image => new GetProductByIdResponseImage(
                Id: image.Id,
                ImagePath: image.ImagePath,
                IsPrimary: image.IsPrimary,
                DisplayOrder: image.DisplayOrder,
                AltText: image.AltText))
            .ToArray();

        return new GetProductByIdResponse(
            Id: result.Id,
            Name: result.Name,
            SeoTitle: result.SeoTitle,
            FullDescription: result.FullDescription,
            MetaDescription: result.MetaDescription,
            Slug: result.Slug,
            Price: result.Price,
            CategoryId: result.CategoryId,
            DisplayOrder: result.DisplayOrder,
            IsFeatured: result.IsFeatured,
            PublishStatus: PublishStatusMapper.ToString(result.PublishStatusCode),
            AvailabilityStatus: AvailabilityStatusMapper.ToString(result.AvailabilityStatusCode),
            Images: images,
            CreatedAt: result.CreatedAt,
            UpdatedAt: result.UpdatedAt,
            DeletedAt: result.DeletedAt,
            RowVersion: result.RowVersion);
    }
}

internal sealed record GetProductByIdResponseImage(
    long Id,
    string ImagePath,
    bool IsPrimary,
    int DisplayOrder,
    string? AltText);
