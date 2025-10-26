using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ListAdminCategories;
using ForeverBloom.Api.Models;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.ListAdminCategories;

public interface IListAdminCategoriesEndpointQueryProvider
{
    Task<List<AdminCategoryListItem>> GetCategoriesAsync(ListAdminCategoriesRequest request, SortCriterion[] sortColumns, CancellationToken cancellationToken = default);
    Task<int> GetCategoriesCountAsync(ListAdminCategoriesRequest request, CancellationToken cancellationToken = default);
}
