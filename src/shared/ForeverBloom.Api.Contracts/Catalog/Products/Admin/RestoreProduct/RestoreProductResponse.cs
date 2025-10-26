namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.RestoreProduct;

public sealed record RestoreProductResponse
{
    public DateTimeOffset? DeletedAt { get; init; }
    public uint RowVersion { get; init; }
}
