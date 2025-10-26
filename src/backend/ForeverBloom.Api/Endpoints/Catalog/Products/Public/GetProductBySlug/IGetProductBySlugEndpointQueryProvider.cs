using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductBySlug;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Public.GetProductBySlug;

public interface IGetProductBySlugEndpointQueryProvider
{
    Task<SlugLookupResult?> GetSlugLookupAsync(string slug, CancellationToken cancellationToken = default);
    Task<GetProductBySlugResponse?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);
}

public sealed record SlugLookupResult
{
    public int ProductId { get; init; }
    public string CurrentSlug { get; init; } = null!;
    public bool IsProvidedSlugCurrent { get; init; }
}
