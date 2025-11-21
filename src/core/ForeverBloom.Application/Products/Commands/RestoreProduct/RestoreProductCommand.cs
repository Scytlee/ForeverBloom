using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.RestoreProduct;

public sealed record RestoreProductCommand : ICommand<RestoreProductResult>
{
    public long ProductId { get; private init; }
    public uint RowVersion { get; private init; }

    private RestoreProductCommand() { }

    public static Result<RestoreProductCommand> Create(
        long productId,
        uint rowVersion)
    {
        var errors = new List<IError>();

        // ProductId (required)
        if (productId <= 0)
        {
            errors.Add(new ProductErrors.ProductIdInvalid(productId));
        }

        // RowVersion (required)
        if (rowVersion <= 0)
        {
            errors.Add(new ApplicationErrors.RowVersionInvalid(rowVersion));
        }

        return Result<RestoreProductCommand>.FromValidation(
            errors,
            () => new RestoreProductCommand
            {
                ProductId = productId,
                RowVersion = rowVersion
            });
    }
}
