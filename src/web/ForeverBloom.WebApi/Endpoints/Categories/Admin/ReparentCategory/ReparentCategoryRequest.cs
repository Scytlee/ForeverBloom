using ForeverBloom.Application.Categories.Commands.ReparentCategory;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.ReparentCategory;

internal sealed record ReparentCategoryRequest(
    Optional<long?> NewParentCategoryId,
    uint RowVersion)
{
    internal Result<ReparentCategoryCommand> ToCommand(long categoryId) =>
        ReparentCategoryCommand.Create(categoryId, RowVersion, NewParentCategoryId);
}
