namespace ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProductImages;

public sealed record UpdateProductImagesRequest
{
    public IReadOnlyList<UpdateProductImageItem> Images { get; init; } = [];
    public uint RowVersion { get; init; }
}

public sealed record UpdateProductImageItem
{
    public string ImagePath { get; init; } = null!;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
    public string? AltText { get; init; }
}
