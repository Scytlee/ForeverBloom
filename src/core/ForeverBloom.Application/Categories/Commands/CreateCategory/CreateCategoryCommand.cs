using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Categories.Commands.CreateCategory;

/// <summary>
/// Command to create a new category.
/// </summary>
public sealed record CreateCategoryCommand(
    string Name,
    string? Description,
    string Slug,
    string? ImagePath,
    string? ImageAltText,
    long? ParentCategoryId,
    int DisplayOrder) : ICommand<CreateCategoryResult>;
