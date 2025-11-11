namespace ForeverBloom.Application.Categories.Commands.ArchiveCategory;

public sealed record ArchiveCategoryResult(
    DateTimeOffset DeletedAt,
    uint RowVersion
);
