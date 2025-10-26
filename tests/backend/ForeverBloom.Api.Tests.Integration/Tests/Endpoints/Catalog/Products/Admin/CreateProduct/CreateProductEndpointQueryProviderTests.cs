using FluentAssertions;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Seeding.Database;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.CreateProduct;

public sealed class CreateProductEndpointQueryProviderTests : DatabaseTestClassBase
{
    public CreateProductEndpointQueryProviderTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task IsSlugAvailableAsync_ShouldReturnTrue_WhenSlugDoesNotExist()
    {
        var sut = new CreateProductEndpointQueryProvider(DbContext);
        var nonExistentSlug = $"non-existent-slug-{_testId:N}"[..20];

        var result = await sut.IsSlugAvailableAsync(nonExistentSlug, TestContext.Current.CancellationToken);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlugAvailableAsync_ShouldReturnFalse_WhenSlugExists()
    {
        var sut = new CreateProductEndpointQueryProvider(DbContext);
        var existingSlug = $"existing-slug-{_testId:N}"[..20];
        await DbContext.CreateSlugRegistryEntryAsync(
            slug: existingSlug,
            entityId: 1,
            entityType: EntityType.Product,
            isActive: true,
            cancellationToken: TestContext.Current.CancellationToken);

        var result = await sut.IsSlugAvailableAsync(existingSlug, TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlugAvailableAsync_ShouldReturnFalse_WhenInactiveSlugExists()
    {
        var sut = new CreateProductEndpointQueryProvider(DbContext);
        var existingSlug = $"inactive-slug-{_testId:N}"[..20];
        await DbContext.CreateSlugRegistryEntryAsync(
            slug: existingSlug,
            entityId: 1,
            entityType: EntityType.Product,
            isActive: false,
            cancellationToken: TestContext.Current.CancellationToken);

        var result = await sut.IsSlugAvailableAsync(existingSlug, TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlugAvailableAsync_ShouldNotTrackEntities()
    {
        var sut = new CreateProductEndpointQueryProvider(DbContext);
        var testSlug = $"track-test-slug-{_testId:N}"[..20];
        await DbContext.CreateSlugRegistryEntryAsync(
            slug: testSlug,
            entityId: 1,
            cancellationToken: TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        await sut.IsSlugAvailableAsync(testSlug, TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task CategoryExistsAsync_ShouldReturnTrue_WhenCategoryExists()
    {
        var sut = new CreateProductEndpointQueryProvider(DbContext);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await sut.CategoryExistsAsync(category.Id, TestContext.Current.CancellationToken);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CategoryExistsAsync_ShouldReturnFalse_WhenCategoryDoesNotExist()
    {
        var sut = new CreateProductEndpointQueryProvider(DbContext);
        const int nonExistentCategoryId = 999999;

        var result = await sut.CategoryExistsAsync(nonExistentCategoryId, TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CategoryExistsAsync_ShouldNotTrackEntities()
    {
        var sut = new CreateProductEndpointQueryProvider(DbContext);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        await sut.CategoryExistsAsync(category.Id, TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Entries().Should().BeEmpty();
    }
}
