using ForeverBloom.Application.Categories.Commands.DeleteCategory;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.DeleteCategory;

internal sealed record DeleteCategoryRequest(
    uint RowVersion)
{
    internal Result<DeleteCategoryCommand> ToCommand(long categoryId) =>
        DeleteCategoryCommand.Create(
            categoryId: categoryId,
            rowVersion: RowVersion);
}
