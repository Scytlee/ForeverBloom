using ForeverBloom.Application.Categories.Commands.ReslugCategory;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.ReslugCategory;

internal sealed record ReslugCategoryRequest(
    string NewSlug,
    uint RowVersion)
{
    internal Result<ReslugCategoryCommand> ToCommand(long categoryId) =>
        ReslugCategoryCommand.Create(
            categoryId: categoryId,
            rowVersion: RowVersion,
            newSlug: NewSlug);
}
