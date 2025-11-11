using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Categories.Commands.ArchiveCategory;

public sealed record ArchiveCategoryCommand(
    long CategoryId,
    uint RowVersion
) : ICommand<ArchiveCategoryResult>;
