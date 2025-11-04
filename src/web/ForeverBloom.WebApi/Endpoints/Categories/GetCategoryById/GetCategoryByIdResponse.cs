using ForeverBloom.Application.Categories.Queries.GetCategoryById;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.Endpoints.Categories.GetCategoryById;

/// <summary>
/// Response payload returned for the admin GetCategoryById endpoint.
/// </summary>
internal sealed record GetCategoryByIdResponse(
    long Id,
    string Name,
    string? Description,
    string Slug,
    string? ImagePath,
    string? ImageAltText,
    long? ParentCategoryId,
    int DisplayOrder,
    string PublishStatus,
    string Path,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt,
    uint RowVersion)
{
    internal static GetCategoryByIdResponse FromResult(GetCategoryByIdResult result)
    {
        return new GetCategoryByIdResponse(
            Id: result.Id,
            Name: result.Name,
            Description: result.Description,
            Slug: result.Slug,
            ImagePath: result.ImagePath,
            ImageAltText: result.ImageAltText,
            ParentCategoryId: result.ParentCategoryId,
            DisplayOrder: result.DisplayOrder,
            PublishStatus: PublishStatusMapper.ToString(result.PublishStatusCode),
            Path: result.Path,
            CreatedAt: result.CreatedAt,
            UpdatedAt: result.UpdatedAt,
            DeletedAt: result.DeletedAt,
            RowVersion: result.RowVersion);
    }
}
