namespace ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;

public sealed class GetCategoryBySlugResult
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public string? ImageSource { get; set; }
    public string? ImageAltText { get; set; }
    public long? ParentCategoryId { get; set; }
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; set; } = null!;
}

public sealed class BreadcrumbItem
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
}
