using Flurl;
using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoriesSitemapData;
using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryBySlug;
using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryTree;
using ForeverBloom.ApiClient.Contracts;
using Microsoft.Extensions.Logging;

namespace ForeverBloom.ApiClient.Endpoints.Categories;

internal sealed class CategoryEndpoints : EndpointsBase<CategoryEndpoints>, ICategoryEndpoints
{
    public CategoryEndpoints(HttpClient httpClient, ILoggerFactory loggerFactory)
        : base(httpClient, loggerFactory.CreateLogger<CategoryEndpoints>())
    {
    }

    public async Task<ApiResponse<GetCategoryTreeResponse>> GetCategoryTreeAsync(int? rootCategoryId = null, int? depth = null, CancellationToken cancellationToken = default)
    {
        var requestUri = "categories/tree"
            .SetQueryParam("rootCategoryId", rootCategoryId, NullValueHandling.Ignore)
            .SetQueryParam("depth", depth, NullValueHandling.Ignore)
            .ToUri();

        Logger.LogDebug("Fetching category tree from {RequestUri}", requestUri);

        return await GetJsonAsync<GetCategoryTreeResponse>(requestUri, cancellationToken);
    }

    public async Task<ApiResponse<GetCategoryBySlugResponse>> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"categories/{Uri.EscapeDataString(slug)}", UriKind.Relative);

        Logger.LogDebug("Fetching category by slug from {RequestUri}", requestUri);

        return await GetJsonAsync<GetCategoryBySlugResponse>(requestUri, cancellationToken);
    }

    public async Task<ApiResponse<GetCategoriesSitemapDataResponse>> GetCategoriesSitemapDataAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri("categories/sitemap-data", UriKind.Relative);

        Logger.LogDebug("Fetching categories sitemap data from {RequestUri}", requestUri);

        return await GetJsonAsync<GetCategoriesSitemapDataResponse>(requestUri, cancellationToken);
    }
}
