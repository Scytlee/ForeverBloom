using ForeverBloom.Application.Products.Commands.ReslugProduct;

namespace ForeverBloom.WebApi.Endpoints.Products.ReslugProduct;

internal sealed record ReslugProductRequest(
    uint RowVersion,
    string NewSlug)
{
    internal ReslugProductCommand ToCommand(long productId) =>
        new(
            ProductId: productId,
            RowVersion: RowVersion,
            NewSlug: NewSlug);
}
