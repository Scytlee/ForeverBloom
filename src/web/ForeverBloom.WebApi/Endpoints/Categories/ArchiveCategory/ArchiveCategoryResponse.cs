using ForeverBloom.Application.Categories.Commands.ArchiveCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.ArchiveCategory;

internal sealed record ArchiveCategoryResponse(
    DateTimeOffset DeletedAt,
    uint RowVersion)
{
    internal static ArchiveCategoryResponse FromResult(ArchiveCategoryResult result)
    {
        return new ArchiveCategoryResponse(
            DeletedAt: result.DeletedAt,
            RowVersion: result.RowVersion);
    }
}
