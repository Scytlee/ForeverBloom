using System.Data;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.ReslugCategory;

public sealed record ReslugCategoryCommand : ICommand<ReslugCategoryResult>, IWithTransactionOverrides
{
    public long CategoryId { get; private init; }
    public Slug NewSlug { get; private init; } = null!;
    public uint RowVersion { get; private init; }

    public TransactionSettings TransactionSettings => new()
    {
        Isolation = IsolationLevel.Serializable,
        LockTimeout = TimeSpan.FromSeconds(2),
        StatementTimeout = TimeSpan.FromSeconds(30)
    };

    private ReslugCategoryCommand() { }

    public static Result<ReslugCategoryCommand> Create(
        long categoryId,
        uint rowVersion,
        string newSlug)
    {
        var errors = new List<IError>();

        // CategoryId (required)
        if (categoryId <= 0)
        {
            errors.Add(new CategoryErrors.CategoryIdInvalid(categoryId));
        }

        // RowVersion (required)
        if (rowVersion <= 0)
        {
            errors.Add(new ApplicationErrors.RowVersionInvalid(rowVersion));
        }

        // NewSlug (required)
        var newSlugResult = Slug.Create(newSlug);
        if (newSlugResult.IsFailure)
        {
            errors.Add(newSlugResult.Error);
        }

        return Result<ReslugCategoryCommand>.FromValidation(
            errors,
            () => new ReslugCategoryCommand
            {
                CategoryId = categoryId,
                NewSlug = newSlugResult.Value!,
                RowVersion = rowVersion
            });
    }
}
