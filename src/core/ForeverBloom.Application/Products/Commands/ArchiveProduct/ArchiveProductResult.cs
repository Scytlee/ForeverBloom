namespace ForeverBloom.Application.Products.Commands.ArchiveProduct;

public sealed record ArchiveProductResult(
    DateTimeOffset DeletedAt,
    uint RowVersion);
