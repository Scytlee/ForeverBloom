using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Domain.Shared.Models;

namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.ListAdminProducts;

public sealed record ListAdminProductsResponse : PaginatedList<ProductListItem>
{
    public ListAdminProductsResponse(IList<ProductListItem> items, int pageNumber, int pageSize, int totalCount)
        : base(items, pageNumber, pageSize, totalCount)
    {
    }

    public ListAdminProductsResponse()
    {
    }
}

public sealed record ProductListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public decimal? Price { get; init; }
    public string? MetaDescription { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsFeatured { get; init; }
    public PublishStatus PublishStatus { get; init; }
    public ProductAvailabilityStatus Availability { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public bool CategoryIsActive { get; init; }
    public string? PrimaryImagePath { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
}
