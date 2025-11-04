using ForeverBloom.Application.Categories.Commands.ReparentCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.ReparentCategory;

internal sealed record ReparentCategoryRequest(
    long? NewParentCategoryId,
    uint RowVersion)
{
    internal ReparentCategoryCommand ToCommand(long categoryId)
    {
        return new ReparentCategoryCommand(
            CategoryId: categoryId,
            RowVersion: RowVersion,
            NewParentCategoryId: NewParentCategoryId);
    }
}
