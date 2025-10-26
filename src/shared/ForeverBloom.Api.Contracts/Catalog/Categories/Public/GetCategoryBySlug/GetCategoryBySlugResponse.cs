namespace ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryBySlug;

public sealed record GetCategoryBySlugResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Slug { get; init; } = null!;
    public string? ImagePath { get; init; }
    public int? ParentCategoryId { get; init; }
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; init; } = null!;
}

public sealed record BreadcrumbItem
{
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
}
