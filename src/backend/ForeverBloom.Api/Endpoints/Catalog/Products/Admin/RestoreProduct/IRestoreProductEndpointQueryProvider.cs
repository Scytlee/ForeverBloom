using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.RestoreProduct;

public interface IRestoreProductEndpointQueryProvider
{
    Task<Product?> GetProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default);
}
