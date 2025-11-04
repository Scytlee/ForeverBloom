using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.Application.Products.Commands.UpdateProductImages;

public sealed record UpdateProductImagesCommand(
    long ProductId,
    uint RowVersion,
    IReadOnlyList<UpdateProductImagesCommand.CreateImageOperation> ImagesToCreate,
    IReadOnlyList<UpdateProductImagesCommand.UpdateImageOperation> ImagesToUpdate,
    IReadOnlyList<long> ImagesToDelete) : ICommand<UpdateProductImagesResult>
{
    public sealed record CreateImageOperation(
        string Source,
        string? AltText,
        bool IsPrimary,
        int DisplayOrder);

    public sealed record UpdateImageOperation(
        long Id,
        Optional<string?> AltText,
        Optional<bool> IsPrimary,
        Optional<int> DisplayOrder);
}
