using ForeverBloom.Application.Categories.Commands.CreateCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.CreateCategory;

/// <summary>
/// Request body for creating a new category via the admin API.
/// </summary>
internal sealed record CreateCategoryRequest(
    string Name,
    string? Description,
    string Slug,
    string? ImagePath,
    string? ImageAltText,
    long? ParentCategoryId,
    int DisplayOrder)
{
    internal CreateCategoryCommand ToCommand() => new(
        Name: Name,
        Description: Description,
        Slug: Slug,
        ImagePath: ImagePath,
        ImageAltText: ImageAltText,
        ParentCategoryId: ParentCategoryId,
        DisplayOrder: DisplayOrder);
}
