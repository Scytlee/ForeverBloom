using ForeverBloom.Api.Contracts.Common;

namespace ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ListAdminCategories;

public sealed class ListAdminCategoriesRequest : PaginationQuery
{
    public string? OrderBy { get; set; }
    public bool? Active { get; set; }
    public string? SearchTerm { get; set; }
    public int? ParentCategoryId { get; set; }
    public bool? IncludeArchived { get; set; }
}
