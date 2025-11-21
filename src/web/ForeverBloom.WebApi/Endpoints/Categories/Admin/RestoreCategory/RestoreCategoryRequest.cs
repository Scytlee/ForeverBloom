using ForeverBloom.Application.Categories.Commands.RestoreCategory;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.RestoreCategory;

internal sealed record RestoreCategoryRequest(
    uint RowVersion)
{
    internal Result<RestoreCategoryCommand> ToCommand(long categoryId) =>
        RestoreCategoryCommand.Create(
            categoryId: categoryId,
            rowVersion: RowVersion);
}
