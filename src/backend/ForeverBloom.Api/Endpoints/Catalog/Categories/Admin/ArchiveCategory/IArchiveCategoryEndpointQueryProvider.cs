using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.ArchiveCategory;

public interface IArchiveCategoryEndpointQueryProvider
{
    Task<Category?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> HasChildCategoriesAsync(int categoryId, CancellationToken cancellationToken = default);
}
