namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.CreateProduct;

public interface ICreateProductEndpointQueryProvider
{
    Task<bool> IsSlugAvailableAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);
}
