using FluentAssertions;
using ForeverBloom.Persistence.Seeding;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Tests.Seeding;

public sealed class DataSeederTests : DatabaseTestClassBase
{
    public DataSeederTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateBaselineCatalogData_WhenDatabaseIsEmpty()
    {
        var sut = new DataSeeder(DbContext);
        var cancellationToken = TestContext.Current.CancellationToken;

        await sut.SeedAsync(cancellationToken);

        DbContext.ChangeTracker.Clear();
        var categoryCount = await DbContext.Categories.AsNoTracking().CountAsync(cancellationToken);
        var productCount = await DbContext.Products.AsNoTracking().CountAsync(cancellationToken);
        var imageCount = await DbContext.ProductImages.AsNoTracking().CountAsync(cancellationToken);
        var slugCount = await DbContext.SlugRegistry.AsNoTracking().CountAsync(cancellationToken);
        categoryCount.Should().NotBe(0);
        productCount.Should().NotBe(0);
        imageCount.Should().NotBe(0);
        slugCount.Should().NotBe(0);
    }

    [Fact]
    public async Task SeedAsync_ShouldNotChangeEntityCounts_WhenExecutedMultipleTimes()
    {
        var sut = new DataSeeder(DbContext);
        var cancellationToken = TestContext.Current.CancellationToken;
        await sut.SeedAsync(cancellationToken);
        DbContext.ChangeTracker.Clear();
        var categoryCountAfterFirstRun = await DbContext.Categories.AsNoTracking().CountAsync(cancellationToken);
        var productCountAfterFirstRun = await DbContext.Products.AsNoTracking().CountAsync(cancellationToken);
        var imageCountAfterFirstRun = await DbContext.ProductImages.AsNoTracking().CountAsync(cancellationToken);
        var slugCountAfterFirstRun = await DbContext.SlugRegistry.AsNoTracking().CountAsync(cancellationToken);

        await sut.SeedAsync(cancellationToken);

        DbContext.ChangeTracker.Clear();
        var categoryCountAfterSecondRun = await DbContext.Categories.AsNoTracking().CountAsync(cancellationToken);
        var productCountAfterSecondRun = await DbContext.Products.AsNoTracking().CountAsync(cancellationToken);
        var imageCountAfterSecondRun = await DbContext.ProductImages.AsNoTracking().CountAsync(cancellationToken);
        var slugCountAfterSecondRun = await DbContext.SlugRegistry.AsNoTracking().CountAsync(cancellationToken);
        categoryCountAfterSecondRun.Should().Be(categoryCountAfterFirstRun);
        productCountAfterSecondRun.Should().Be(productCountAfterFirstRun);
        imageCountAfterSecondRun.Should().Be(imageCountAfterFirstRun);
        slugCountAfterSecondRun.Should().Be(slugCountAfterFirstRun);
    }
}
