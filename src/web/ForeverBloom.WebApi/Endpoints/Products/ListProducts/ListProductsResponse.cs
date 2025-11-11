using ForeverBloom.Application.Products.Queries.ListProducts;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.Endpoints.Products.ListProducts;

internal sealed record ListProductsResponse(
    IReadOnlyList<ListProductsResponseItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    internal static ListProductsResponse FromResult(ListProductsResult result)
    {
        var items = result.Items
            .Select(item => new ListProductsResponseItem(
                Id: item.Id,
                Name: item.Name,
                Slug: item.Slug,
                Price: item.Price,
                MetaDescription: item.MetaDescription,
                IsFeatured: item.IsFeatured,
                PublishStatus: PublishStatusMapper.ToString(item.PublishStatus),
                Availability: AvailabilityStatusMapper.ToString(item.Availability),
                CategoryId: item.CategoryId,
                CategoryName: item.CategoryName,
                CategoryPublishStatus: PublishStatusMapper.ToString(item.CategoryPublishStatus),
                ImageSource: item.ImageSource,
                ImageAltText: item.ImageAltText,
                CreatedAt: item.CreatedAt,
                UpdatedAt: item.UpdatedAt,
                DeletedAt: item.DeletedAt))
            .ToArray();

        return new ListProductsResponse(
            Items: items,
            PageNumber: result.PageNumber,
            PageSize: result.PageSize,
            TotalCount: result.TotalCount,
            TotalPages: result.TotalPages);
    }
}

internal sealed record ListProductsResponseItem(
    long Id,
    string Name,
    string Slug,
    decimal? Price,
    string? MetaDescription,
    bool IsFeatured,
    string PublishStatus,
    string Availability,
    long CategoryId,
    string CategoryName,
    string CategoryPublishStatus,
    string? ImageSource,
    string? ImageAltText,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);
