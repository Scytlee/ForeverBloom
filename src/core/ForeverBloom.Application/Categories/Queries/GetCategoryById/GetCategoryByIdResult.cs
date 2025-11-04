namespace ForeverBloom.Application.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdResult
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public string? ImagePath { get; set; }
    public string? ImageAltText { get; set; }
    public long? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public int PublishStatusCode { get; set; }
    public string Path { get; set; } = null!;

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public uint RowVersion { get; set; }
}
