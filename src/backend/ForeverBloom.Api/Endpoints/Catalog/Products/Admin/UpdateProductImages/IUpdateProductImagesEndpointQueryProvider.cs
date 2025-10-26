using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProductImages;

public interface IUpdateProductImagesEndpointQueryProvider
{
    Task<Product?> GetProductAsync(int productId, CancellationToken cancellationToken = default);
}
