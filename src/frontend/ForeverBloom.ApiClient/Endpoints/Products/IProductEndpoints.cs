using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductBySlug;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductsSitemapData;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.ListProducts;
using ForeverBloom.ApiClient.Contracts;

namespace ForeverBloom.ApiClient.Endpoints.Products;

public interface IProductEndpoints
{
    Task<ApiResponse<ListProductsResponse>> ListProductsAsync(ListProductsRequest? request = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<GetProductBySlugResponse>> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ApiResponse<GetProductsSitemapDataResponse>> GetProductsSitemapDataAsync(CancellationToken cancellationToken = default);
}
