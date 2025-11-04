namespace ForeverBloom.Application.Categories.Commands.ReparentCategory;

public sealed record ReparentCategoryResult(
    string Path,
    long? ParentCategoryId,
    DateTimeOffset UpdatedAt,
    uint RowVersion);
