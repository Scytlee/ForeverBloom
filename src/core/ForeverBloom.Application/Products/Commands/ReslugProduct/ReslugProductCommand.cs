using System.Data;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.ReslugProduct;

public sealed record ReslugProductCommand : ICommand<ReslugProductResult>, IWithTransactionOverrides
{
    public long ProductId { get; private init; }
    public uint RowVersion { get; private init; }
    public Slug NewSlug { get; private init; } = null!;

    public TransactionSettings TransactionSettings { get; } = new()
    {
        Isolation = IsolationLevel.Serializable,
        LockTimeout = TimeSpan.FromSeconds(2),
        StatementTimeout = TimeSpan.FromSeconds(30)
    };

    public static Result<ReslugProductCommand> Create(long productId, uint rowVersion, string newSlug)
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

        var slugResult = Slug.Create(newSlug);
        if (slugResult.IsFailure)
        {
            errors.Add(slugResult.Error);
        }

        return Result<ReslugProductCommand>.FromValidation(
            errors,
            () => new ReslugProductCommand
            {
                ProductId = productId,
                RowVersion = rowVersion,
                NewSlug = slugResult.Value!
            });
    }
}
