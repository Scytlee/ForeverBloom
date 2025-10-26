using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductsSitemapData;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Public.GetProductsSitemapData;

public interface IGetProductsSitemapDataEndpointQueryProvider
{
    Task<List<ProductSitemapDataItem>> GetProductsSitemapDataAsync(CancellationToken cancellationToken = default);
}
