namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.DeleteProduct;

public interface IDeleteProductEndpointQueryProvider
{
    Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> ProductIsArchivedAsync(int productId, CancellationToken cancellationToken = default);
}
