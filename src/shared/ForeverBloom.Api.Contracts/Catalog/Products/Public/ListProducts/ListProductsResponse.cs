using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Domain.Shared.Models;

namespace ForeverBloom.Api.Contracts.Catalog.Products.Public.ListProducts;

public sealed record ListProductsResponse : PaginatedList<PublicProductListItem>
{
    public ListProductsResponse(IList<PublicProductListItem> items, int pageNumber, int pageSize, int totalCount)
        : base(items, pageNumber, pageSize, totalCount)
    {
    }

    public ListProductsResponse()
    {
    }
}

public sealed record PublicProductListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public decimal? Price { get; init; }
    public string? MetaDescription { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public string? PrimaryImagePath { get; init; }
    public ProductAvailabilityStatus Availability { get; init; }
    public bool IsFeatured { get; init; }
}
