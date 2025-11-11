using ForeverBloom.Application.Categories.Commands.RestoreCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.RestoreCategory;

internal sealed record RestoreCategoryRequest(
    uint RowVersion)
{
    internal RestoreCategoryCommand ToCommand(long categoryId)
    {
        return new RestoreCategoryCommand(
            CategoryId: categoryId,
            RowVersion: RowVersion);
    }
}
