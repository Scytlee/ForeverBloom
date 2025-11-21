using ForeverBloom.Application.Categories.Commands.UpdateCategory;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.UpdateCategory;

internal sealed record UpdateCategoryRequest(
    uint RowVersion,
    Optional<string> Name,
    Optional<string?> Description,
    Optional<string?> ImagePath,
    Optional<string?> ImageAltText,
    Optional<int> DisplayOrder,
    Optional<string> PublishStatus)
{
    internal Result<UpdateCategoryCommand> ToCommand(long categoryId) =>
        UpdateCategoryCommand.Create(
            categoryId: categoryId,
            rowVersion: RowVersion,
            name: Name,
            description: Description,
            imagePath: ImagePath,
            imageAltText: ImageAltText,
            displayOrder: DisplayOrder,
            publishStatus: PublishStatus);
}
