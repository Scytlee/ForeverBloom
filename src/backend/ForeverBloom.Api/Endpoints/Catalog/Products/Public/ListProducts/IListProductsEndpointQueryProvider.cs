using ForeverBloom.Api.Contracts.Catalog.Products.Public.ListProducts;
using ForeverBloom.Api.Models;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Public.ListProducts;

public interface IListProductsEndpointQueryProvider
{
    Task<List<PublicProductListItem>> GetProductsAsync(ListProductsRequest request, SortCriterion[] sortColumns, CancellationToken cancellationToken = default);
    Task<int> GetProductsCountAsync(ListProductsRequest request, CancellationToken cancellationToken = default);
}
