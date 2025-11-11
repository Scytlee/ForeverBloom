using ForeverBloom.Application.Products.Queries.GetProductsSitemapData;

namespace ForeverBloom.WebApi.Endpoints.Products.GetProductsSitemapData;

internal sealed record GetProductsSitemapDataResponse(
    IReadOnlyList<ProductSitemapDataItemResponse> Items)
{
    internal static GetProductsSitemapDataResponse FromResult(GetProductsSitemapDataResult result)
    {
        var items = result.Items
            .Select(item => new ProductSitemapDataItemResponse(
                Slug: item.Slug,
                UpdatedAt: item.UpdatedAt))
            .ToArray();

        return new GetProductsSitemapDataResponse(items);
    }
}

internal sealed record ProductSitemapDataItemResponse(
    string Slug,
    DateTimeOffset UpdatedAt);
