using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Testing.Common.Seeding.Database;

public static class ProductImageDatabaseSeedingHelper
{
    public static ProductImage CreateProductImageWithoutSaving(
        int productId,
        string? imagePath = null,
        bool isPrimary = false,
        int displayOrder = 0,
        string? altText = null)
    {
        var token = Guid.NewGuid().ToString("N");

        return new ProductImage
        {
            ProductId = productId,
            ImagePath = imagePath ?? $"/images/product-{token[..20]}.jpg",
            IsPrimary = isPrimary,
            DisplayOrder = displayOrder,
            AltText = altText ?? $"Alt text {token[..10]}"
        };
    }

    public static async Task<ProductImage> CreateProductImageAsync(
        this ApplicationDbContext context,
        int productId,
        string? imagePath = null,
        bool isPrimary = false,
        int displayOrder = 0,
        string? altText = null,
        CancellationToken cancellationToken = default)
    {
        var productImage = CreateProductImageWithoutSaving(productId, imagePath, isPrimary, displayOrder, altText);
        context.ProductImages.Add(productImage);
        await context.SaveChangesAsync(cancellationToken);
        return productImage;
    }
}
