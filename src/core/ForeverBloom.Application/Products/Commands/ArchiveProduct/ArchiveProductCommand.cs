using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.ArchiveProduct;

public sealed record ArchiveProductCommand : ICommand<ArchiveProductResult>
{
    public long ProductId { get; private init; }
    public uint RowVersion { get; private init; }

    private ArchiveProductCommand() { }

    public static Result<ArchiveProductCommand> Create(
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

        return Result<ArchiveProductCommand>.FromValidation(
            errors,
            () => new ArchiveProductCommand
            {
                ProductId = productId,
                RowVersion = rowVersion
            });
    }
}
