using ForeverBloom.Application.Products.Commands.RestoreProduct;

namespace ForeverBloom.WebApi.Endpoints.Products.RestoreProduct;

internal sealed record RestoreProductResponse(
    DateTimeOffset? DeletedAt,
    uint RowVersion)
{
    internal static RestoreProductResponse FromResult(RestoreProductResult result) =>
        new(result.DeletedAt, result.RowVersion);
}
