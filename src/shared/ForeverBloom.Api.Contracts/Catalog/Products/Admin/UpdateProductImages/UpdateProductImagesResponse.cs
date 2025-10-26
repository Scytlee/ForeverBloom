namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProductImages;

public sealed record UpdateProductImagesResponse
{
    public IReadOnlyList<ProductImageItem> Images { get; init; } = [];
    public uint RowVersion { get; init; }
}

public sealed record ProductImageItem
{
    public string ImagePath { get; init; } = null!;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
    public string? AltText { get; init; }
}
