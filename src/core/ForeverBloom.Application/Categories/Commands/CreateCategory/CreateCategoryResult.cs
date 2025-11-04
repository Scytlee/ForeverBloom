namespace ForeverBloom.Application.Categories.Commands.CreateCategory;

/// <summary>
/// Result payload returned after successfully creating a category.
/// </summary>
public sealed record CreateCategoryResult(long CategoryId);
