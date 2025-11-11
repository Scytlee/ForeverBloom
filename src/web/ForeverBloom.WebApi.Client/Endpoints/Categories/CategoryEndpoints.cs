using Flurl;
using ForeverBloom.WebApi.Client.Contracts;
using ForeverBloom.WebApi.Client.Endpoints.Categories.Contracts;
using Microsoft.Extensions.Logging;

namespace ForeverBloom.WebApi.Client.Endpoints.Categories;

internal sealed class CategoryEndpoints : EndpointsBase<CategoryEndpoints>, ICategoryEndpoints
{
    public CategoryEndpoints(HttpClient httpClient, ILoggerFactory loggerFactory)
        : base(httpClient, loggerFactory.CreateLogger<CategoryEndpoints>())
    {
    }

    public async Task<HttpResult<BrowseCatalogCategoryTreeResponse>> BrowseCatalogCategoryTreeAsync(BrowseCatalogCategoryTreeRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new BrowseCatalogCategoryTreeRequest();

        var requestUri = "categories/tree"
            .SetQueryParam("RootCategoryId", request.RootCategoryId, NullValueHandling.Ignore)
            .SetQueryParam("Depth", request.Depth, NullValueHandling.Ignore)
            .ToUri();

        Logger.LogDebug("Fetching catalog category tree from {RequestUri}", requestUri);

        return await GetJsonAsync<BrowseCatalogCategoryTreeResponse>(requestUri, cancellationToken);
    }

    public async Task<HttpResult<GetCategoryBySlugResponse>> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"categories/{Uri.EscapeDataString(slug)}", UriKind.Relative);

        Logger.LogDebug("Fetching category by slug from {RequestUri}", requestUri);

        return await GetJsonAsync<GetCategoryBySlugResponse>(requestUri, cancellationToken);
    }

    public async Task<HttpResult<GetCategoriesSitemapDataResponse>> GetCategoriesSitemapDataAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri("categories/sitemap-data", UriKind.Relative);

        Logger.LogDebug("Fetching categories sitemap data from {RequestUri}", requestUri);

        return await GetJsonAsync<GetCategoriesSitemapDataResponse>(requestUri, cancellationToken);
    }
}
