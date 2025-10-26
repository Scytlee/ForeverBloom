using ForeverBloom.Api.Contracts.Common;

namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.ListAdminProducts;

public sealed class ListAdminProductsRequest : PaginationQuery
{
    public string? OrderBy { get; set; }
    public bool? ProductActive { get; set; }
    public bool? CategoryActive { get; set; }
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public bool? IncludeSubcategories { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IncludeArchived { get; set; }
}
