using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    long CategoryId,
    uint RowVersion,
    Optional<string> Name,
    Optional<string?> Description,
    Optional<string?> ImagePath,
    Optional<string?> ImageAltText,
    Optional<int> DisplayOrder,
    Optional<PublishStatus> PublishStatus) : ICommand<UpdateCategoryResult>;
