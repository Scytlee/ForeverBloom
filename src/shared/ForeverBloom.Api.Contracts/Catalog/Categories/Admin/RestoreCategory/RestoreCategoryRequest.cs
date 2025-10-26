namespace ForeverBloom.Api.Contracts.Catalog.Categories.Admin.RestoreCategory;

public sealed record RestoreCategoryRequest
{
    public uint RowVersion { get; init; }
}
