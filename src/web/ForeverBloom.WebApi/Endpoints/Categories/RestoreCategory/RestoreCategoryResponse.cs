using ForeverBloom.Application.Categories.Commands.RestoreCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.RestoreCategory;

internal sealed record RestoreCategoryResponse(
    DateTimeOffset? DeletedAt,
    uint RowVersion)
{
    internal static RestoreCategoryResponse FromResult(RestoreCategoryResult result)
    {
        return new RestoreCategoryResponse(
            DeletedAt: result.DeletedAt,
            RowVersion: result.RowVersion);
    }
}
