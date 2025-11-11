namespace ForeverBloom.Application.Categories.Commands.RestoreCategory;

public sealed record RestoreCategoryResult(
    DateTimeOffset? DeletedAt,
    uint RowVersion
);
