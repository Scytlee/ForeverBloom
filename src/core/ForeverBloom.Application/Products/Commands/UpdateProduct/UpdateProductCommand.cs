using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    long ProductId,
    uint RowVersion,
    Optional<string> Name,
    Optional<string?> SeoTitle,
    Optional<string?> FullDescription,
    Optional<string?> MetaDescription,
    Optional<long> CategoryId,
    Optional<decimal?> Price,
    Optional<int> DisplayOrder,
    Optional<bool> IsFeatured,
    Optional<ProductAvailabilityStatus> Availability,
    Optional<PublishStatus> PublishStatus) : ICommand<UpdateProductResult>;
