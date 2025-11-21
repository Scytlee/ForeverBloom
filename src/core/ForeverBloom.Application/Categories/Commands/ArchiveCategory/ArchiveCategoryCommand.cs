using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.ArchiveCategory;

public sealed record ArchiveCategoryCommand : ICommand<ArchiveCategoryResult>
{
    public long CategoryId { get; private init; }
    public uint RowVersion { get; private init; }

    private ArchiveCategoryCommand() { }

    public static Result<ArchiveCategoryCommand> Create(
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

        return Result<ArchiveCategoryCommand>.FromValidation(
            errors,
            () => new ArchiveCategoryCommand
            {
                CategoryId = categoryId,
                RowVersion = rowVersion
            });
    }
}
