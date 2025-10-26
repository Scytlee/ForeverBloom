using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Testing.Common.Seeding.Database;

public static class ProductDatabaseSeedingHelper
{
    public static Product CreateProductWithoutSaving(
        int categoryId,
        string? name = null,
        string? slug = null,
        decimal? price = null,
        bool isFeatured = false,
        PublishStatus publishStatus = PublishStatus.Draft,
        ProductAvailabilityStatus availability = ProductAvailabilityStatus.Available)
    {
        var token = Guid.NewGuid().ToString("N");

        return new Product
        {
            Name = name ?? $"Product {token[..20]}",
            SeoTitle = $"SEO {token[..15]}",
            FullDescription = $"Full description for {token[..10]}",
            MetaDescription = $"Meta description for {token[..10]}",
            CurrentSlug = slug ?? $"product-{token[..20]}",
            Price = price,
            DisplayOrder = 0,
            IsFeatured = isFeatured,
            PublishStatus = publishStatus,
            Availability = availability,
            CategoryId = categoryId
        };
    }

    public static async Task<Product> CreateProductAsync(
        this ApplicationDbContext context,
        int categoryId,
        string? name = null,
        string? slug = null,
        decimal? price = null,
        bool isFeatured = false,
        PublishStatus publishStatus = PublishStatus.Draft,
        ProductAvailabilityStatus availability = ProductAvailabilityStatus.Available,
        CancellationToken cancellationToken = default)
    {
        var product = CreateProductWithoutSaving(categoryId, name, slug, price, isFeatured, publishStatus, availability);
        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);
        return product;
    }
}
