using ForeverBloom.Application.Products.Commands.UpdateProduct;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.Endpoints.Products.UpdateProduct;

internal sealed record UpdateProductResponse(
    string Name,
    string? SeoTitle,
    string? FullDescription,
    string? MetaDescription,
    long CategoryId,
    decimal? Price,
    int DisplayOrder,
    bool IsFeatured,
    string Availability,
    string PublishStatus,
    DateTimeOffset UpdatedAt,
    uint RowVersion)
{
    internal static UpdateProductResponse FromResult(UpdateProductResult result)
    {
        return new UpdateProductResponse(
            Name: result.Name,
            SeoTitle: result.SeoTitle,
            FullDescription: result.FullDescription,
            MetaDescription: result.MetaDescription,
            CategoryId: result.CategoryId,
            Price: result.Price,
            DisplayOrder: result.DisplayOrder,
            IsFeatured: result.IsFeatured,
            Availability: AvailabilityStatusMapper.ToString(result.Availability),
            PublishStatus: PublishStatusMapper.ToString(result.PublishStatus),
            UpdatedAt: result.UpdatedAt,
            RowVersion: result.RowVersion);
    }
}
