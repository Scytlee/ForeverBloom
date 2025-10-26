using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.UpdateCategory;

public interface IUpdateCategoryEndpointQueryProvider
{
    Task<Category?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> CategoryNameExistsAtSameLevelAsync(string name, int? parentCategoryId, int excludeCategoryId, CancellationToken cancellationToken = default);
    Task<bool> CategoryHasChildrenAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> IsSlugAvailableAsync(string slug, int excludeCategoryId, CancellationToken cancellationToken = default);
    Task<string?> GetCategoryPathAsync(int categoryId, CancellationToken cancellationToken = default);
}
