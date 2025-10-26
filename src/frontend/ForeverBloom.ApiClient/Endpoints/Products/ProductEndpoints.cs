using Flurl;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductBySlug;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductsSitemapData;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.ListProducts;
using ForeverBloom.ApiClient.Contracts;
using Microsoft.Extensions.Logging;

namespace ForeverBloom.ApiClient.Endpoints.Products;

internal sealed class ProductEndpoints : EndpointsBase<ProductEndpoints>, IProductEndpoints
{
    public ProductEndpoints(HttpClient httpClient, ILoggerFactory loggerFactory)
        : base(httpClient, loggerFactory.CreateLogger<ProductEndpoints>())
    {
    }

    public async Task<ApiResponse<ListProductsResponse>> ListProductsAsync(ListProductsRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new ListProductsRequest();

        var requestUri = "products"
            .SetQueryParam("PageNumber", request.PageNumber, NullValueHandling.Ignore)
            .SetQueryParam("PageSize", request.PageSize, NullValueHandling.Ignore)
            .SetQueryParam("OrderBy", request.OrderBy, NullValueHandling.Ignore)
            .SetQueryParam("SearchTerm", request.SearchTerm, NullValueHandling.Ignore)
            .SetQueryParam("CategoryId", request.CategoryId, NullValueHandling.Ignore)
            .SetQueryParam("IncludeSubcategories", request.IncludeSubcategories, NullValueHandling.Ignore)
            .SetQueryParam("Featured", request.Featured, NullValueHandling.Ignore)
            .ToUri();

        Logger.LogDebug("Fetching products from {RequestUri}", requestUri);

        return await GetJsonAsync<ListProductsResponse>(requestUri, cancellationToken);
    }

    public async Task<ApiResponse<GetProductBySlugResponse>> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"products/{Uri.EscapeDataString(slug)}", UriKind.Relative);

        Logger.LogDebug("Fetching product by slug from {RequestUri}", requestUri);

        return await GetJsonAsync<GetProductBySlugResponse>(requestUri, cancellationToken);
    }

    public async Task<ApiResponse<GetProductsSitemapDataResponse>> GetProductsSitemapDataAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri("products/sitemap-data", UriKind.Relative);

        Logger.LogDebug("Fetching products sitemap data from {RequestUri}", requestUri);

        return await GetJsonAsync<GetProductsSitemapDataResponse>(requestUri, cancellationToken);
    }
}
