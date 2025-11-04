using ForeverBloom.Application.Products.Commands.UpdateProductImages;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.WebApi.Endpoints.Products.UpdateProductImages;

internal sealed record UpdateProductImagesRequest(
    uint RowVersion,
    IReadOnlyList<UpdateProductImagesRequest.CreateImageOperation>? Create,
    IReadOnlyList<UpdateProductImagesRequest.UpdateImageOperation>? Update,
    IReadOnlyList<long>? Delete)
{
    internal UpdateProductImagesCommand ToCommand(long productId)
    {
        var create = Create?
            .Select(image => new UpdateProductImagesCommand.CreateImageOperation(
                image.Source,
                image.AltText,
                image.IsPrimary,
                image.DisplayOrder))
            .ToArray() ?? [];

        var update = Update?
            .Select(image => new UpdateProductImagesCommand.UpdateImageOperation(
                image.Id,
                image.AltText,
                image.IsPrimary,
                image.DisplayOrder))
            .ToArray() ?? [];

        var delete = Delete?.ToArray() ?? [];

        return new UpdateProductImagesCommand(
            ProductId: productId,
            RowVersion: RowVersion,
            ImagesToCreate: create,
            ImagesToUpdate: update,
            ImagesToDelete: delete);
    }

    internal sealed record CreateImageOperation(
        string Source,
        string? AltText,
        bool IsPrimary,
        int DisplayOrder);

    internal sealed record UpdateImageOperation(
        long Id,
        Optional<string?> AltText,
        Optional<bool> IsPrimary,
        Optional<int> DisplayOrder);
}
