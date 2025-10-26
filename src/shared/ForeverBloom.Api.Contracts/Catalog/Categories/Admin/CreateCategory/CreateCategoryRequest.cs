namespace ForeverBloom.Api.Contracts.Catalog.Categories.Admin.CreateCategory;

public sealed record CreateCategoryRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Slug { get; init; } = null!;
    public string? ImagePath { get; init; }
    public int? ParentCategoryId { get; init; }
    public int DisplayOrder { get; init; } = 0;
    public bool IsActive { get; init; } = true;
}
