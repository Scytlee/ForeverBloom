using ForeverBloom.Api.Contracts.Catalog.Products.Admin.ListAdminProducts;
using ForeverBloom.Api.Models;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.ListAdminProducts;

public interface IListAdminProductsEndpointQueryProvider
{
    Task<List<ProductListItem>> GetProductsAsync(ListAdminProductsRequest request, SortCriterion[] sortColumns, CancellationToken cancellationToken = default);
    Task<int> GetProductsCountAsync(ListAdminProductsRequest request, CancellationToken cancellationToken = default);
}
