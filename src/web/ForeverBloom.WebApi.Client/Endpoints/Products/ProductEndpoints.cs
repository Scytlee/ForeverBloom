using Flurl;
using ForeverBloom.WebApi.Client.Contracts;
using ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;
using Microsoft.Extensions.Logging;

namespace ForeverBloom.WebApi.Client.Endpoints.Products;

internal sealed class ProductEndpoints : EndpointsBase<ProductEndpoints>, IProductEndpoints
{
    public ProductEndpoints(HttpClient httpClient, ILoggerFactory loggerFactory)
        : base(httpClient, loggerFactory.CreateLogger<ProductEndpoints>())
    {
    }

    public async Task<HttpResult<BrowseCatalogProductsResponse>> BrowseCatalogProductsAsync(BrowseCatalogProductsRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new BrowseCatalogProductsRequest();

        var requestUri = "products"
            .SetQueryParam("PageNumber", request.PageNumber, NullValueHandling.Ignore)
            .SetQueryParam("PageSize", request.PageSize, NullValueHandling.Ignore)
            .SetQueryParam("Sort", request.Sort, NullValueHandling.Ignore)
            .SetQueryParam("CategoryId", request.CategoryId, NullValueHandling.Ignore)
            .SetQueryParam("Featured", request.Featured, NullValueHandling.Ignore)
            .ToUri();

        Logger.LogDebug("Fetching catalog products from {RequestUri}", requestUri);

        return await GetJsonAsync<BrowseCatalogProductsResponse>(requestUri, cancellationToken);
    }

    public async Task<HttpResult<GetProductBySlugResponse>> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"products/{Uri.EscapeDataString(slug)}", UriKind.Relative);

        Logger.LogDebug("Fetching product by slug from {RequestUri}", requestUri);

        return await GetJsonAsync<GetProductBySlugResponse>(requestUri, cancellationToken);
    }

    public async Task<HttpResult<GetProductsSitemapDataResponse>> GetProductsSitemapDataAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri("products/sitemap-data", UriKind.Relative);

        Logger.LogDebug("Fetching products sitemap data from {RequestUri}", requestUri);

        return await GetJsonAsync<GetProductsSitemapDataResponse>(requestUri, cancellationToken);
    }
}
