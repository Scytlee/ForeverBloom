using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand : ICommand
{
    public long CategoryId { get; private init; }
    public uint RowVersion { get; private init; }

    private DeleteCategoryCommand() { }

    public static Result<DeleteCategoryCommand> Create(
        long categoryId,
        uint rowVersion)
    {
        var errors = new List<IError>();

        if (categoryId <= 0)
        {
            errors.Add(new CategoryErrors.CategoryIdInvalid(categoryId));
        }

        if (rowVersion <= 0)
        {
            errors.Add(new ApplicationErrors.RowVersionInvalid(rowVersion));
        }

        return Result<DeleteCategoryCommand>.FromValidation(
            errors,
            () => new DeleteCategoryCommand
            {
                CategoryId = categoryId,
                RowVersion = rowVersion
            });
    }
}
