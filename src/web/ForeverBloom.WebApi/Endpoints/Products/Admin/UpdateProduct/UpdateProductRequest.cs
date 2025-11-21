using ForeverBloom.Application.Products.Commands.UpdateProduct;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.UpdateProduct;

internal sealed record UpdateProductRequest(
    uint RowVersion,
    Optional<string> Name,
    Optional<string?> SeoTitle,
    Optional<string?> FullDescription,
    Optional<string?> MetaDescription,
    Optional<long> CategoryId,
    Optional<decimal?> Price,
    Optional<bool> IsFeatured,
    Optional<string> Availability,
    Optional<string> PublishStatus)
{
    internal Result<UpdateProductCommand> ToCommand(long productId) =>
        UpdateProductCommand.Create(
            productId: productId,
            rowVersion: RowVersion,
            name: Name,
            seoTitle: SeoTitle,
            fullDescription: FullDescription,
            metaDescription: MetaDescription,
            categoryId: CategoryId,
            price: Price,
            isFeatured: IsFeatured,
            availability: Availability,
            publishStatus: PublishStatus);
}
