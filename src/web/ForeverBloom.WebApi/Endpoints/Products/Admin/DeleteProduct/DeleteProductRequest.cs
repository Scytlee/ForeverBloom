using ForeverBloom.Application.Products.Commands.DeleteProduct;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.DeleteProduct;

internal sealed record DeleteProductRequest(
    uint RowVersion)
{
    internal Result<DeleteProductCommand> ToCommand(long productId) =>
        DeleteProductCommand.Create(
            productId: productId,
            rowVersion: RowVersion);
}
