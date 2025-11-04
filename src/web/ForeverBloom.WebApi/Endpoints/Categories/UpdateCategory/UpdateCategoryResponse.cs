using ForeverBloom.Application.Categories.Commands.UpdateCategory;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.Endpoints.Categories.UpdateCategory;

internal sealed record UpdateCategoryResponse(
    string Name,
    string? Description,
    string? ImagePath,
    string? ImageAltText,
    int DisplayOrder,
    string PublishStatus,
    DateTimeOffset UpdatedAt,
    uint RowVersion)
{
    internal static UpdateCategoryResponse FromResult(UpdateCategoryResult result)
    {
        return new UpdateCategoryResponse(
            Name: result.Name,
            Description: result.Description,
            ImagePath: result.ImagePath,
            ImageAltText: result.ImageAltText,
            DisplayOrder: result.DisplayOrder,
            PublishStatus: PublishStatusMapper.ToString(result.PublishStatus),
            UpdatedAt: result.UpdatedAt,
            RowVersion: result.RowVersion);
    }
}
