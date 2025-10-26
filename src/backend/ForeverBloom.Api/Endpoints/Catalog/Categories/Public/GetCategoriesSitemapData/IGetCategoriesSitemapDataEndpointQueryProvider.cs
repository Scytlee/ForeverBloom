using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoriesSitemapData;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoriesSitemapData;

public interface IGetCategoriesSitemapDataEndpointQueryProvider
{
    Task<List<CategorySitemapDataItem>> GetCategoriesSitemapDataAsync(CancellationToken cancellationToken = default);
}
