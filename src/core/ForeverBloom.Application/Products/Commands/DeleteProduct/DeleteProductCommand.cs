using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand : ICommand
{
    public long ProductId { get; private init; }
    public uint RowVersion { get; private init; }

    private DeleteProductCommand() { }

    public static Result<DeleteProductCommand> Create(
        long productId,
        uint rowVersion)
    {
        var errors = new List<IError>();

        if (productId <= 0)
        {
            errors.Add(new ProductErrors.ProductIdInvalid(productId));
        }

        if (rowVersion <= 0)
        {
            errors.Add(new ApplicationErrors.RowVersionInvalid(rowVersion));
        }

        return Result<DeleteProductCommand>.FromValidation(
            errors,
            () => new DeleteProductCommand
            {
                ProductId = productId,
                RowVersion = rowVersion
            });
    }
}
