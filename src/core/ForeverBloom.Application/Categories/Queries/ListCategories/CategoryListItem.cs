namespace ForeverBloom.Application.Categories.Queries.ListCategories;

public sealed class CategoryListItem
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public long? ParentCategoryId { get; set; }
    public string? Path { get; set; }
    public int DisplayOrder { get; set; }
    public int PublishStatus { get; set; }
    public string? ImageSource { get; set; }
    public string? ImageAltText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
