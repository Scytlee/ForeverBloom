namespace ForeverBloom.Application.Products.Commands.RestoreProduct;

public sealed record RestoreProductResult(
    DateTimeOffset? DeletedAt,
    uint RowVersion);
