using ForeverBloom.Application.Products.Commands.DeleteProduct;

namespace ForeverBloom.WebApi.Endpoints.Products.DeleteProduct;

internal sealed record DeleteProductRequest(
    uint RowVersion)
{
    internal DeleteProductCommand ToCommand(long productId) =>
        new(ProductId: productId, RowVersion: RowVersion);
}
