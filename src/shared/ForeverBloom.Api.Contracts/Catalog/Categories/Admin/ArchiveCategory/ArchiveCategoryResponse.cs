namespace ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ArchiveCategory;

public sealed record ArchiveCategoryResponse
{
    public DateTimeOffset DeletedAt { get; init; }
    public uint RowVersion { get; init; }
}
