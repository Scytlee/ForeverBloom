using ForeverBloom.Application.Products.Commands.CreateProduct;

namespace ForeverBloom.WebApi.Endpoints.Products.CreateProduct;

/// <summary>
/// Response returned after successfully creating a product.
/// </summary>
internal sealed record CreateProductResponse(long Id)
{
    internal static CreateProductResponse FromResult(CreateProductResult result) => new(result.Id);
}
