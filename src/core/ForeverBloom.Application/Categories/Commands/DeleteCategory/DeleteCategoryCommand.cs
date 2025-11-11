using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(
    long CategoryId,
    uint RowVersion) : ICommand;
