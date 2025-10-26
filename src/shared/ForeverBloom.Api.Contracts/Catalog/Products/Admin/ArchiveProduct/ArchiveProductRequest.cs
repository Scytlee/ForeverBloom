namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.ArchiveProduct;

public sealed record ArchiveProductRequest
{
    public uint RowVersion { get; init; }
}
