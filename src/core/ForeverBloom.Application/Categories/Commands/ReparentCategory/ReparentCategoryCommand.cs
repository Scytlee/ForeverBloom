using System.Data;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.ReparentCategory;

public sealed record ReparentCategoryCommand : ICommand<ReparentCategoryResult>, IWithTransactionOverrides
{
    public long CategoryId { get; private init; }
    public long? NewParentCategoryId { get; private init; }
    public uint RowVersion { get; private init; }

    public TransactionSettings TransactionSettings { get; } = new()
    {
        Isolation = IsolationLevel.Serializable,
        LockTimeout = TimeSpan.FromSeconds(2),
        StatementTimeout = TimeSpan.FromSeconds(30)
    };

    private ReparentCategoryCommand() { }

    public static Result<ReparentCategoryCommand> Create(
        long categoryId,
        uint rowVersion,
        Optional<long?> newParentCategoryId)
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

        // NewParentCategoryId (must be provided)
        if (!newParentCategoryId.IsSet)
        {
            errors.Add(new ApplicationErrors.RequiredFieldMissing(nameof(NewParentCategoryId)));
        }
        else if (newParentCategoryId.Value.HasValue)
        {
            // When provided and not null, must be > 0
            if (newParentCategoryId.Value.Value <= 0)
            {
                errors.Add(new CategoryErrors.ParentCategoryIdInvalid(newParentCategoryId.Value.Value));
            }

            // Cannot be own parent
            if (newParentCategoryId.Value.Value == categoryId)
            {
                errors.Add(new CategoryErrors.CannotBeOwnParent(categoryId));
            }
        }

        return Result<ReparentCategoryCommand>.FromValidation(
            errors,
            () => new ReparentCategoryCommand
            {
                CategoryId = categoryId,
                NewParentCategoryId = newParentCategoryId.Value,
                RowVersion = rowVersion
            });
    }
}
