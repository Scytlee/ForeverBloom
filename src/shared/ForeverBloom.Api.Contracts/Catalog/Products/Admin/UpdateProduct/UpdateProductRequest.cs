using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Domain.Shared.Models;

namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProduct;

public sealed record UpdateProductRequest
{
    public Optional<string> Name { get; init; }
    public Optional<string?> SeoTitle { get; init; }
    public Optional<string?> FullDescription { get; init; }
    public Optional<string?> MetaDescription { get; init; }
    public Optional<string> Slug { get; init; }
    public Optional<decimal?> Price { get; init; }
    public Optional<int> CategoryId { get; init; }
    public Optional<int> DisplayOrder { get; init; }
    public Optional<bool> IsFeatured { get; init; }
    public Optional<PublishStatus> PublishStatus { get; init; }
    public Optional<ProductAvailabilityStatus> Availability { get; init; }
    public uint RowVersion { get; init; }
}
