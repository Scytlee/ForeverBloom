using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.RestoreCategory;

public sealed record RestoreCategoryCommand : ICommand<RestoreCategoryResult>
{
    public long CategoryId { get; private init; }
    public uint RowVersion { get; private init; }

    private RestoreCategoryCommand() { }

    public static Result<RestoreCategoryCommand> Create(
        long categoryId,
        uint rowVersion)
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

        return Result<RestoreCategoryCommand>.FromValidation(
            errors,
            () => new RestoreCategoryCommand
            {
                CategoryId = categoryId,
                RowVersion = rowVersion
            });
    }
}
