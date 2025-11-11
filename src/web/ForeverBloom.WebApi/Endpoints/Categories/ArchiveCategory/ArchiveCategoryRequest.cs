using ForeverBloom.Application.Categories.Commands.ArchiveCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.ArchiveCategory;

internal sealed record ArchiveCategoryRequest(
    uint RowVersion)
{
    internal ArchiveCategoryCommand ToCommand(long categoryId)
    {
        return new ArchiveCategoryCommand(
            CategoryId: categoryId,
            RowVersion: RowVersion);
    }
}
