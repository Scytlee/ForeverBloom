using ForeverBloom.WebApi.Client.Contracts;
using ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;

namespace ForeverBloom.WebApi.Client.Endpoints.Products;

public interface IProductEndpoints
{
    Task<HttpResult<BrowseCatalogProductsResponse>> BrowseCatalogProductsAsync(BrowseCatalogProductsRequest? request = null, CancellationToken cancellationToken = default);
    Task<HttpResult<GetProductBySlugResponse>> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<HttpResult<GetProductsSitemapDataResponse>> GetProductsSitemapDataAsync(CancellationToken cancellationToken = default);
}
