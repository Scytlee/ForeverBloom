namespace ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryTree;

public sealed record GetCategoryTreeResponse(
    IReadOnlyList<CategoryTreeItem> Categories
);

public sealed record CategoryTreeItem(
    int Id,
    string Name,
    string Slug,
    string? ImagePath,
    IReadOnlyList<CategoryTreeItem> Children
);
