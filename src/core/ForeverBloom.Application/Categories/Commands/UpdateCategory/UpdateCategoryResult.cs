using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryResult(
    string Name,
    string? Description,
    string? ImagePath,
    string? ImageAltText,
    int DisplayOrder,
    PublishStatus PublishStatus,
    DateTimeOffset UpdatedAt,
    uint RowVersion);
