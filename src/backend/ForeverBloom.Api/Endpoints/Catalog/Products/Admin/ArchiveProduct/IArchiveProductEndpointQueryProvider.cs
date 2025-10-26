using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.ArchiveProduct;

public interface IArchiveProductEndpointQueryProvider
{
    Task<Product?> GetProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default);
}
