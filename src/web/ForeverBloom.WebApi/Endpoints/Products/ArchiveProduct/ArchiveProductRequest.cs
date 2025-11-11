using ForeverBloom.Application.Products.Commands.ArchiveProduct;

namespace ForeverBloom.WebApi.Endpoints.Products.ArchiveProduct;

internal sealed record ArchiveProductRequest(
    uint RowVersion)
{
    internal ArchiveProductCommand ToCommand(long productId) =>
        new(ProductId: productId, RowVersion: RowVersion);
}
