namespace ForeverBloom.Application.Categories.Commands.ReslugCategory;

public sealed record ReslugCategoryResult(
    string Slug,
    string Path,
    DateTimeOffset UpdatedAt,
    uint RowVersion);
