using ForeverBloom.Application.Categories.Commands.ArchiveCategory;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.ArchiveCategory;

internal sealed record ArchiveCategoryRequest(
    uint RowVersion)
{
    internal Result<ArchiveCategoryCommand> ToCommand(long categoryId) =>
        ArchiveCategoryCommand.Create(
            categoryId: categoryId,
            rowVersion: RowVersion);
}
