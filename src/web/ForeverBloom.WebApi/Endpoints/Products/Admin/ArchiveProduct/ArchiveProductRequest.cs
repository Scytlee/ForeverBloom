using ForeverBloom.Application.Products.Commands.ArchiveProduct;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.ArchiveProduct;

internal sealed record ArchiveProductRequest(uint RowVersion)
{
    internal Result<ArchiveProductCommand> ToCommand(long productId) =>
        ArchiveProductCommand.Create(
            productId: productId,
            rowVersion: RowVersion);
}
