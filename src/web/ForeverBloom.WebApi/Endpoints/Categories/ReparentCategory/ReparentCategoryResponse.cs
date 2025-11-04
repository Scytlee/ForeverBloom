using ForeverBloom.Application.Categories.Commands.ReparentCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.ReparentCategory;

internal sealed record ReparentCategoryResponse(
    string Path,
    long? ParentCategoryId,
    DateTimeOffset UpdatedAt,
    uint RowVersion)
{
    internal static ReparentCategoryResponse FromResult(ReparentCategoryResult result)
    {
        return new ReparentCategoryResponse(
            Path: result.Path,
            ParentCategoryId: result.ParentCategoryId,
            UpdatedAt: result.UpdatedAt,
            RowVersion: result.RowVersion);
    }
}
