using FluentAssertions;
using ForeverBloom.Persistence.Entities;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Seeding.Database;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Tests.Configurations;

public sealed class ProductConfigurationTests : DatabaseTestClassBase
{
    public ProductConfigurationTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task ProductTable_ShouldCascadeDeleteToProductImages()
    {
        var category = await DbContext.CreateCategoryAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var product = await DbContext.CreateProductAsync(
            categoryId: category.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        await DbContext.CreateProductImageAsync(
            productId: product.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        await DbContext.CreateProductImageAsync(
            productId: product.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var productToDelete = new Product { Id = product.Id, RowVersion = product.RowVersion };
        DbContext.Products.Remove(productToDelete);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var remainingImages = await DbContext.ProductImages
            .AsNoTracking()
            .Where(pi => pi.ProductId == product.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        remainingImages.Should().BeEmpty();
    }
}
