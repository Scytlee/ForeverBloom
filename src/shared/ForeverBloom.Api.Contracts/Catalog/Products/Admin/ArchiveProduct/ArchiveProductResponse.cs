namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.ArchiveProduct;

public sealed record ArchiveProductResponse
{
    public DateTimeOffset DeletedAt { get; init; }
    public uint RowVersion { get; init; }
}
