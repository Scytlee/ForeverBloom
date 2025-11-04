using ForeverBloom.Application.Categories.Commands.ReslugCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.ReslugCategory;

internal sealed record ReslugCategoryResponse(
    string Slug,
    string Path,
    DateTimeOffset UpdatedAt,
    uint RowVersion)
{
    internal static ReslugCategoryResponse FromResult(ReslugCategoryResult result)
    {
        return new ReslugCategoryResponse(
            Slug: result.Slug,
            Path: result.Path,
            UpdatedAt: result.UpdatedAt,
            RowVersion: result.RowVersion);
    }
}
