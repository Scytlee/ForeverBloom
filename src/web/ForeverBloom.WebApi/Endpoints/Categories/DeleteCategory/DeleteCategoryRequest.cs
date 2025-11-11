using ForeverBloom.Application.Categories.Commands.DeleteCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.DeleteCategory;

internal sealed record DeleteCategoryRequest(
    uint RowVersion)
{
    internal DeleteCategoryCommand ToCommand(long categoryId) =>
        new(CategoryId: categoryId, RowVersion: RowVersion);
}
