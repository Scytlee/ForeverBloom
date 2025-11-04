using ForeverBloom.Application.Categories.Commands.CreateCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories.CreateCategory;

/// <summary>
/// Response payload returned to clients after creating a category via the admin API.
/// </summary>
internal sealed record CreateCategoryResponse(long Id)
{
    internal static CreateCategoryResponse FromResult(CreateCategoryResult result) => new(result.CategoryId);
}
