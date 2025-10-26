namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.RestoreProduct;

public sealed record RestoreProductRequest
{
    public uint RowVersion { get; init; }
}
