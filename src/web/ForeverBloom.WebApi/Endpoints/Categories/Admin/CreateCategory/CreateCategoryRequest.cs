using ForeverBloom.Application.Categories.Commands.CreateCategory;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.CreateCategory;

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
    internal Result<CreateCategoryCommand> ToCommand() =>
        CreateCategoryCommand.Create(
            name: Name,
            slug: Slug,
            description: Description,
            imagePath: ImagePath,
            imageAltText: ImageAltText,
            parentCategoryId: ParentCategoryId,
            displayOrder: DisplayOrder);
}
