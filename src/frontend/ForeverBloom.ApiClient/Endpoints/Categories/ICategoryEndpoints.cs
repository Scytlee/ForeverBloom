using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoriesSitemapData;
using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryBySlug;
using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryTree;
using ForeverBloom.ApiClient.Contracts;

namespace ForeverBloom.ApiClient.Endpoints.Categories;

public interface ICategoryEndpoints
{
    Task<ApiResponse<GetCategoryTreeResponse>> GetCategoryTreeAsync(int? rootCategoryId = null, int? depth = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<GetCategoryBySlugResponse>> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ApiResponse<GetCategoriesSitemapDataResponse>> GetCategoriesSitemapDataAsync(CancellationToken cancellationToken = default);
}
