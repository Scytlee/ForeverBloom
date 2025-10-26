using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.GetAdminCategoryById;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.GetAdminCategoryById;

public interface IGetAdminCategoryByIdEndpointQueryProvider
{
    Task<GetAdminCategoryByIdResponse?> GetCategoryByIdAsync(int categoryId, bool includeArchived = false, CancellationToken cancellationToken = default);
}
