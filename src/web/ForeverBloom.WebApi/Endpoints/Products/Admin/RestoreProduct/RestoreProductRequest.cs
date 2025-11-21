using ForeverBloom.Application.Products.Commands.RestoreProduct;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.RestoreProduct;

internal sealed record RestoreProductRequest(
    uint RowVersion)
{
    internal Result<RestoreProductCommand> ToCommand(long productId) =>
        RestoreProductCommand.Create(
            productId: productId,
            rowVersion: RowVersion);
}
