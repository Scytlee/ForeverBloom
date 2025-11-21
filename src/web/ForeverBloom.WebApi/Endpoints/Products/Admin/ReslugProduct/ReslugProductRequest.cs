using ForeverBloom.Application.Products.Commands.ReslugProduct;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.ReslugProduct;

internal sealed record ReslugProductRequest(
    uint RowVersion,
    string NewSlug)
{
    internal Result<ReslugProductCommand> ToCommand(long productId) =>
        ReslugProductCommand.Create(
            productId: productId,
            rowVersion: RowVersion,
            newSlug: NewSlug);
}
