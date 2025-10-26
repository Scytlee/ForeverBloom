using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.RestoreCategory;

public interface IRestoreCategoryEndpointQueryProvider
{
    Task<Category?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);
}
