using ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.Endpoints.Products.BrowseCatalogProducts;

internal sealed record BrowseCatalogProductsResponse(
    IReadOnlyList<BrowseCatalogProductsResponseItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    internal static BrowseCatalogProductsResponse FromResult(BrowseCatalogProductsResult result)
    {
        var items = result.Items
            .Select(item => new BrowseCatalogProductsResponseItem(
                Id: item.Id,
                Name: item.Name,
                Slug: item.Slug,
                Price: item.Price,
                MetaDescription: item.MetaDescription,
                CategoryId: item.CategoryId,
                CategoryName: item.CategoryName,
                ImageSource: item.ImageSource,
                ImageAltText: item.ImageAltText,
                AvailabilityStatus: AvailabilityStatusMapper.ToString(item.AvailabilityStatusCode),
                IsFeatured: item.IsFeatured))
            .ToArray();

        return new BrowseCatalogProductsResponse(
            Items: items,
            PageNumber: result.PageNumber,
            PageSize: result.PageSize,
            TotalCount: result.TotalCount,
            TotalPages: result.TotalPages);
    }
}

internal sealed record BrowseCatalogProductsResponseItem(
    long Id,
    string Name,
    string Slug,
    decimal? Price,
    string? MetaDescription,
    long CategoryId,
    string CategoryName,
    string? ImageSource,
    string? ImageAltText,
    string AvailabilityStatus,
    bool IsFeatured);
