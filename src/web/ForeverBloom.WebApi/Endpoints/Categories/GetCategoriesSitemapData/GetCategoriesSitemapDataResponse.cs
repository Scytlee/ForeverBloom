using ForeverBloom.Application.Categories.Queries.GetCategoriesSitemapData;

namespace ForeverBloom.WebApi.Endpoints.Categories.GetCategoriesSitemapData;

internal sealed record GetCategoriesSitemapDataResponse(
    IReadOnlyList<CategorySitemapDataItemResponse> Items)
{
    internal static GetCategoriesSitemapDataResponse FromResult(GetCategoriesSitemapDataResult result)
    {
        var items = result.Items
            .Select(item => new CategorySitemapDataItemResponse(
                Slug: item.Slug,
                UpdatedAt: item.UpdatedAt))
            .ToArray();

        return new GetCategoriesSitemapDataResponse(items);
    }
}

internal sealed record CategorySitemapDataItemResponse(
    string Slug,
    DateTimeOffset UpdatedAt);
