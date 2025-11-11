using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Categories.Commands.RestoreCategory;

public sealed record RestoreCategoryCommand(
    long CategoryId,
    uint RowVersion
) : ICommand<RestoreCategoryResult>;
