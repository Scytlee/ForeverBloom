using FluentAssertions;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Repositories;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Seeding.Database;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Tests.Repositories;

public sealed class ProductRepositoryTests : DatabaseTestClassBase
{
    public ProductRepositoryTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task InsertProduct_ShouldPersistProductInDatabase()
    {
        var sut = new ProductRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var product = ProductDatabaseSeedingHelper.CreateProductWithoutSaving(
            categoryId: category.Id,
            name: $"Test Product {_testId:N}"[..20],
            slug: $"test-product-{_testId:N}"[..20]);

        sut.InsertProduct(product);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var productInDatabase = await DbContext.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        productInDatabase.Should().NotBeNull();

        AssertionHelpers.AssertAllPropertiesAreMapped(
            sourceObject: product,
            destinationObject: productInDatabase,
            sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(product.Category),
                nameof(product.Images)
            },
            equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));
    }

    [Fact]
    public async Task UpdateProduct_ShouldUpdateProductInDatabase()
    {
        var sut = new ProductRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var product = await DbContext.CreateProductAsync(
            categoryId: category.Id,
            name: $"Original Product {_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);
        var originalRowVersion = product.RowVersion;

        // Modify product properties
        product.Name = $"Updated Product {_testId:N}"[..20];
        product.Price = 199.99m;
        product.IsFeatured = true;
        product.PublishStatus = PublishStatus.Published;

        DbContext.Attach(product);
        sut.UpdateProduct(product);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var productInDatabase = await DbContext.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        productInDatabase.Should().NotBeNull();

        AssertionHelpers.AssertAllPropertiesAreMapped(
            sourceObject: product,
            destinationObject: productInDatabase,
            sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(product.Category),
                nameof(product.Images)
            },
            equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        productInDatabase.RowVersion.Should().NotBe(originalRowVersion);
        productInDatabase.UpdatedAt.Should().BeAfter(productInDatabase.CreatedAt);
    }

    [Fact]
    public async Task ArchiveProduct_ShouldSetDeletedAtInDatabase()
    {
        var sut = new ProductRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var product = await DbContext.CreateProductAsync(
            categoryId: category.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        var lowerBound = TimeProvider.System.GetUtcNow();
        DbContext.Attach(product);
        sut.ArchiveProduct(product);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var upperBound = TimeProvider.System.GetUtcNow();

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var productInDatabase = await DbContext.Products
            .IgnoreQueryFilters()
            .SingleAsync(p => p.Id == product.Id, TestContext.Current.CancellationToken);
        productInDatabase.Should().NotBeNull();
        productInDatabase.DeletedAt.Should().NotBeNull();
        productInDatabase.DeletedAt.Should().BeOnOrAfter(lowerBound);
        productInDatabase.DeletedAt.Should().BeOnOrBefore(upperBound);

        AssertionHelpers.AssertAllPropertiesAreMapped(
            sourceObject: product,
            destinationObject: productInDatabase,
            sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(product.Category),
                nameof(product.Images),
                nameof(product.DeletedAt), // We verify DeletedAt separately above
                nameof(product.UpdatedAt) // UpdatedAt will be different due to the archive operation
            },
            equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        // Verify product is filtered out by default query
        DbContext.ChangeTracker.Clear();
        var productViaFilteredQuery = await DbContext.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        productViaFilteredQuery.Should().BeNull();
    }

    [Fact]
    public async Task RestoreProduct_ShouldClearDeletedAtInDatabase()
    {
        var sut = new ProductRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var product = await DbContext.CreateProductAsync(
            categoryId: category.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        // First archive the product
        DbContext.Attach(product);
        sut.ArchiveProduct(product);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Then restore it
        sut.RestoreProduct(product);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var productInDatabase = await DbContext.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        productInDatabase.Should().NotBeNull();
        productInDatabase.DeletedAt.Should().BeNull();

        AssertionHelpers.AssertAllPropertiesAreMapped(
            sourceObject: product,
            destinationObject: productInDatabase,
            sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(product.Category),
                nameof(product.Images),
                nameof(product.DeletedAt), // Product still has DeletedAt set, but DB entity should be null
                nameof(product.UpdatedAt) // UpdatedAt will be different due to the restore operation
            },
            equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        // Verify product is once again queryable via default query
        DbContext.ChangeTracker.Clear();
        var productViaFilteredQuery = await DbContext.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        productViaFilteredQuery.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteProduct_ShouldRemoveProductFromDatabase()
    {
        var sut = new ProductRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var product = await DbContext.CreateProductAsync(
            categoryId: category.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        var productId = product.Id;
        var rowVersion = product.RowVersion;
        DbContext.ChangeTracker.Clear();

        sut.DeleteProduct(productId, rowVersion);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify product is completely removed from database
        var productInDatabase = await DbContext.Products
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(p => p.Id == productId, TestContext.Current.CancellationToken);
        productInDatabase.Should().BeNull();
    }

    [Fact]
    public async Task ReplaceProductImages_ShouldReplaceAllImagesInDatabase()
    {
        var sut = new ProductRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var product = await DbContext.CreateProductAsync(
            categoryId: category.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        // Add initial images
        await DbContext.CreateProductImageAsync(
            productId: product.Id,
            imagePath: "/initial/image1.jpg",
            isPrimary: true,
            displayOrder: 0,
            altText: "Initial Image 1",
            cancellationToken: TestContext.Current.CancellationToken);
        await DbContext.CreateProductImageAsync(
            productId: product.Id,
            imagePath: "/initial/image2.jpg",
            isPrimary: false,
            displayOrder: 1,
            altText: "Initial Image 2",
            cancellationToken: TestContext.Current.CancellationToken);

        // Create new images to replace existing ones
        var newImages = new[]
        {
            ProductImageDatabaseSeedingHelper.CreateProductImageWithoutSaving(
                productId: product.Id,
                imagePath: "/new/image1.jpg",
                isPrimary: true,
                displayOrder: 0,
                altText: "New Image 1"),
            ProductImageDatabaseSeedingHelper.CreateProductImageWithoutSaving(
                productId: product.Id,
                imagePath: "/new/image2.jpg",
                isPrimary: false,
                displayOrder: 1,
                altText: "New Image 2"),
            ProductImageDatabaseSeedingHelper.CreateProductImageWithoutSaving(
                productId: product.Id,
                imagePath: "/new/image3.jpg",
                isPrimary: false,
                displayOrder: 2,
                altText: "New Image 3")
        };

        DbContext.Attach(product);
        sut.ReplaceProductImages(product, newImages);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var productInDatabase = await DbContext.Products
            .Include(p => p.Images)
            .SingleAsync(p => p.Id == product.Id, TestContext.Current.CancellationToken);

        productInDatabase.Images.Should().HaveCount(3);
        productInDatabase.Images.Should().Contain(img => img.ImagePath == "/new/image1.jpg" && img.IsPrimary);
        productInDatabase.Images.Should().Contain(img => img.ImagePath == "/new/image2.jpg" && !img.IsPrimary);
        productInDatabase.Images.Should().Contain(img => img.ImagePath == "/new/image3.jpg" && !img.IsPrimary);
        productInDatabase.Images.Should().NotContain(img => img.ImagePath.StartsWith("/initial/"));
    }
}
