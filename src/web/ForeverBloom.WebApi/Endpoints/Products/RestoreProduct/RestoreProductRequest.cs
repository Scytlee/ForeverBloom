using ForeverBloom.Application.Products.Commands.RestoreProduct;

namespace ForeverBloom.WebApi.Endpoints.Products.RestoreProduct;

internal sealed record RestoreProductRequest(
    uint RowVersion)
{
    internal RestoreProductCommand ToCommand(long productId) =>
        new(ProductId: productId, RowVersion: RowVersion);
}
