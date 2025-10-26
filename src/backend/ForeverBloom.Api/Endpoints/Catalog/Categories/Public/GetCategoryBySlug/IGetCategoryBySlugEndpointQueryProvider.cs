using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryBySlug;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoryBySlug;

public interface IGetCategoryBySlugEndpointQueryProvider
{
    Task<SlugLookupResult?> GetSlugLookupAsync(string slug, CancellationToken cancellationToken = default);
    Task<GetCategoryBySlugResponse?> GetCategoryByIdAsync(int categoryId, CancellationToken cancellationToken = default);
}

public sealed record SlugLookupResult
{
    public int CategoryId { get; init; }
    public string CurrentSlug { get; init; } = null!;
    public bool IsProvidedSlugCurrent { get; init; }
}
