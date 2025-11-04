namespace ForeverBloom.Application.Products.Commands.ReslugProduct;

public sealed record ReslugProductResult(
    string CurrentSlug,
    DateTimeOffset UpdatedAt,
    uint RowVersion);
