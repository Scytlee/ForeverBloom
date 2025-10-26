namespace ForeverBloom.Api.Contracts.Catalog.Categories.Admin.GetAdminCategoryById;

public sealed record GetAdminCategoryByIdResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Slug { get; init; } = null!;
    public string? ImagePath { get; init; }
    public int? ParentCategoryId { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public string? Path { get; init; }

    // Audit fields
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
    public uint RowVersion { get; init; }
}
