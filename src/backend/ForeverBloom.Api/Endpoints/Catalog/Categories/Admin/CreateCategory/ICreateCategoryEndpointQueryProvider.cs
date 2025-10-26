namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.CreateCategory;

public interface ICreateCategoryEndpointQueryProvider
{
    Task<bool> IsSlugAvailableAsync(string slug, CancellationToken cancellationToken = default);
    Task<string?> GetCategoryPathByIdAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> CategoryNameExistsWithinParentAsync(string name, int? parentCategoryId, CancellationToken cancellationToken = default);
    Task<int> GetParentHierarchyDepthAsync(int parentCategoryId, CancellationToken cancellationToken = default);
}
