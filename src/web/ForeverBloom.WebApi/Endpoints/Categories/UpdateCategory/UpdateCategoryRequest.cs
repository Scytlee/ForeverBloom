using ForeverBloom.Application.Categories.Commands.UpdateCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.WebApi.Endpoints.Categories.UpdateCategory;

internal sealed record UpdateCategoryRequest(
    uint RowVersion,
    Optional<string> Name,
    Optional<string?> Description,
    Optional<string?> ImagePath,
    Optional<string?> ImageAltText,
    Optional<int> DisplayOrder,
    Optional<string> PublishStatus)
{
    internal UpdateCategoryCommand ToCommand(
        long categoryId,
        PublishStatus? publishStatus)
    {
        return new UpdateCategoryCommand(
            CategoryId: categoryId,
            RowVersion: RowVersion,
            Name: Name,
            Description: Description,
            ImagePath: ImagePath,
            ImageAltText: ImageAltText,
            DisplayOrder: DisplayOrder,
            PublishStatus: publishStatus is not null
                ? Optional<PublishStatus>.FromValue(publishStatus)
                : Optional<PublishStatus>.Unset);
    }
}
