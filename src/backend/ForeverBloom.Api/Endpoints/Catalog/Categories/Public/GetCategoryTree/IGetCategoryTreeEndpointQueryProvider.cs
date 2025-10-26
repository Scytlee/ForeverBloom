using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryTree;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoryTree;

public interface IGetCategoryTreeEndpointQueryProvider
{
    Task<GetCategoryTreeResponse> GetCategoryTreeAsync(int? rootCategoryId = null, int? depth = null, CancellationToken cancellationToken = default);
}
