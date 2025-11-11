using ForeverBloom.WebApi.Client.Contracts;
using ForeverBloom.WebApi.Client.Endpoints.Categories.Contracts;

namespace ForeverBloom.WebApi.Client.Endpoints.Categories;

public interface ICategoryEndpoints
{
    Task<HttpResult<BrowseCatalogCategoryTreeResponse>> BrowseCatalogCategoryTreeAsync(BrowseCatalogCategoryTreeRequest? request = null, CancellationToken cancellationToken = default);
    Task<HttpResult<GetCategoryBySlugResponse>> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<HttpResult<GetCategoriesSitemapDataResponse>> GetCategoriesSitemapDataAsync(CancellationToken cancellationToken = default);
}
