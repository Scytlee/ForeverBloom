using ForeverBloom.Application.Categories.Commands.ReslugCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.ReslugCategory;

internal sealed record ReslugCategoryRequest(
    string NewSlug,
    uint RowVersion)
{
    internal ReslugCategoryCommand ToCommand(long categoryId)
    {
        return new ReslugCategoryCommand(
            CategoryId: categoryId,
            NewSlug: NewSlug,
            RowVersion: RowVersion);
    }
}
