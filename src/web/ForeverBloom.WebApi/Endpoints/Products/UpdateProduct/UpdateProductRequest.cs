using ForeverBloom.Application.Products.Commands.UpdateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.WebApi.Endpoints.Products.UpdateProduct;

internal sealed record UpdateProductRequest(
    uint RowVersion,
    Optional<string> Name,
    Optional<string?> SeoTitle,
    Optional<string?> FullDescription,
    Optional<string?> MetaDescription,
    Optional<long> CategoryId,
    Optional<decimal?> Price,
    Optional<int> DisplayOrder,
    Optional<bool> IsFeatured,
    Optional<string> Availability,
    Optional<string> PublishStatus)
{
    internal UpdateProductCommand ToCommand(
        long productId,
        ProductAvailabilityStatus? availabilityStatus,
        PublishStatus? publishStatus)
    {
        return new UpdateProductCommand(
            ProductId: productId,
            RowVersion: RowVersion,
            Name: Name,
            SeoTitle: SeoTitle,
            FullDescription: FullDescription,
            MetaDescription: MetaDescription,
            CategoryId: CategoryId,
            Price: Price,
            DisplayOrder: DisplayOrder,
            IsFeatured: IsFeatured,
            Availability: availabilityStatus is not null
                ? Optional<ProductAvailabilityStatus>.FromValue(availabilityStatus)
                : Optional<ProductAvailabilityStatus>.Unset,
            PublishStatus: publishStatus is not null
                ? Optional<PublishStatus>.FromValue(publishStatus)
                : Optional<PublishStatus>.Unset);
    }
}
