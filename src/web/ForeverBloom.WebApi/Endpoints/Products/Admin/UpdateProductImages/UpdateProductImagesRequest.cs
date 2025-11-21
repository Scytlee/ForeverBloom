using ForeverBloom.Application.Products.Commands.UpdateProductImages;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.UpdateProductImages;

internal sealed record UpdateProductImagesRequest(
    uint RowVersion,
    IReadOnlyList<UpdateProductImagesRequest.CreateImageOperation>? Create,
    IReadOnlyList<UpdateProductImagesRequest.UpdateImageOperation>? Update,
    IReadOnlyList<long>? Delete)
{
    internal Result<UpdateProductImagesCommand> ToCommand(long productId)
    {
        var create = Create?
            .Select(image => new UpdateProductImagesCommand.CreateImageOperation(
                image.Source,
                image.AltText,
                image.IsPrimary,
                image.DisplayOrder))
            .ToArray();

        var update = Update?
            .Select(image => new UpdateProductImagesCommand.UpdateImageOperation(
                image.Id,
                image.AltText,
                image.IsPrimary,
                image.DisplayOrder))
            .ToArray();

        var delete = Delete?.ToArray();

        return UpdateProductImagesCommand.Create(
            productId: productId,
            rowVersion: RowVersion,
            imagesToCreate: create,
            imagesToUpdate: update,
            imagesToDelete: delete);
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
