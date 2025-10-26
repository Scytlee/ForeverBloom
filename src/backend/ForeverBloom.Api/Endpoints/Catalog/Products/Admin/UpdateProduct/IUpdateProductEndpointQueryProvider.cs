using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProduct;

public interface IUpdateProductEndpointQueryProvider
{
    Task<Product?> GetProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> IsSlugAvailableAsync(string slug, int excludeProductId, CancellationToken cancellationToken = default);
    Task<CategoryInfo?> GetCategoryInfoAsync(int categoryId, CancellationToken cancellationToken = default);
}

public sealed record CategoryInfo
{
    public string Name { get; init; } = null!;
    public string CurrentSlug { get; init; } = null!;
}
