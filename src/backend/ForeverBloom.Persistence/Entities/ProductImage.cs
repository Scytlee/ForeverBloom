namespace ForeverBloom.Persistence.Entities;

public sealed class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ImagePath { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
    public string? AltText { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
}
