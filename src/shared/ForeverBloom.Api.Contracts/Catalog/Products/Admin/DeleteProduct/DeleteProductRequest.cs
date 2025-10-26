namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.DeleteProduct;

public sealed record DeleteProductRequest
{
    public uint? RowVersion { get; init; }
}
