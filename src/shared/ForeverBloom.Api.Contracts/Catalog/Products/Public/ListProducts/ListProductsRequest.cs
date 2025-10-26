using ForeverBloom.Api.Contracts.Common;

namespace ForeverBloom.Api.Contracts.Catalog.Products.Public.ListProducts;

public sealed class ListProductsRequest : PaginationQuery
{
    public string? OrderBy { get; set; }
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public bool? IncludeSubcategories { get; set; }
    public bool? Featured { get; set; }
}
