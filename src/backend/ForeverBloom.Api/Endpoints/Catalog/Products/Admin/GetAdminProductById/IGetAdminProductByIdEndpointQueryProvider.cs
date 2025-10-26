using ForeverBloom.Api.Contracts.Catalog.Products.Admin.GetAdminProductById;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.GetAdminProductById;

public interface IGetAdminProductByIdEndpointQueryProvider
{
    Task<GetAdminProductByIdResponse?> GetProductByIdAsync(int productId, bool includeArchived = false, CancellationToken cancellationToken = default);
}
