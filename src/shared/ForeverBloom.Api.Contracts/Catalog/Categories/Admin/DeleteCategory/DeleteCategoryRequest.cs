namespace ForeverBloom.Api.Contracts.Catalog.Categories.Admin.DeleteCategory;

public sealed record DeleteCategoryRequest
{
    public uint? RowVersion { get; init; }
}
