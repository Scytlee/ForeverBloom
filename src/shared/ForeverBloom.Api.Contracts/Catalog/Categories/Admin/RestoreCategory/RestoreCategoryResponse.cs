namespace ForeverBloom.Api.Contracts.Catalog.Categories.Admin.RestoreCategory;

public sealed record RestoreCategoryResponse
{
    public DateTimeOffset? DeletedAt { get; init; }
    public uint RowVersion { get; init; }
}
