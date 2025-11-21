using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Products.Commands.ReslugProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.SlugRegistry;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Products.Commands;

public sealed class ReslugProductUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task ReslugProduct_ShouldUpdateSlug_WhenSlugChanges()
    {
        var originalSlug = $"original-{TestToken}";
        var newSlug = $"updated-{TestToken}";

        var category = await Fixture.GivenCategoryAsync();
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: originalSlug);

        var commandResult = ReslugProductCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            newSlug: newSlug);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.CurrentSlug.Should().Be(newSlug);
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.CurrentSlug.Should().HaveValue(newSlug);
        productAfter.RowVersion.Should().Be(payload.RowVersion);
        productAfter.UpdatedAt.Should().Be(payload.UpdatedAt);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Product && r.EntityId == product.Id)
                .OrderBy(r => r.Id)
                .ToArrayAsync(ct));

        slugRegistrations.Should().HaveCount(2);
        slugRegistrations.Should().Contain(r => r.Slug.Value == originalSlug && !r.IsActive);
        slugRegistrations.Should().Contain(r => r.Slug.Value == newSlug && r.IsActive);
    }

    [Fact]
    public async Task ReslugProduct_ShouldReactivateHistoricalSlug_WhenReverting()
    {
        var originalSlug = $"initial-{TestToken}";
        var temporarySlug = $"temporary-{TestToken}";

        var category = await Fixture.GivenCategoryAsync();
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: temporarySlug,
            slugHistory: [originalSlug]);

        var commandResult = ReslugProductCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            newSlug: originalSlug);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.CurrentSlug.Should().Be(originalSlug);
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.CurrentSlug.Should().HaveValue(originalSlug);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Product && r.EntityId == product.Id)
                .OrderBy(r => r.Id)
                .ToArrayAsync(ct));

        slugRegistrations.Should().HaveCount(2);
        slugRegistrations.Should().Contain(r => r.Slug.Value == originalSlug && r.IsActive);
        slugRegistrations.Should().Contain(r => r.Slug.Value == temporarySlug && !r.IsActive);
    }

    [Fact]
    public async Task ReslugProduct_ShouldReturnExistingValues_WhenSlugUnchanged()
    {
        var category = await Fixture.GivenCategoryAsync();
        var product = await Fixture.GivenProductAsync(categoryId: category.Id);

        var commandResult = ReslugProductCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            newSlug: product.CurrentSlug.Value);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.CurrentSlug.Should().Be(product.CurrentSlug.Value);
        payload.RowVersion.Should().Be(product.RowVersion);
        payload.UpdatedAt.Should().Be(product.UpdatedAt);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.RowVersion.Should().Be(product.RowVersion);
        productAfter.CurrentSlug.Should().HaveValue(product.CurrentSlug.Value);
        productAfter.UpdatedAt.Should().Be(product.UpdatedAt);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Product && r.EntityId == product.Id)
                .ToArrayAsync(ct));

        slugRegistrations.Should().ContainSingle(r => r.IsActive);
    }

    [Fact]
    public async Task ReslugProduct_ShouldFail_WhenSlugBelongsToADifferentEntityType()
    {
        var sharedSlug = $"shared-{TestToken}";

        var category = await Fixture.GivenCategoryAsync(
            slug: sharedSlug);

        var productCategory = await Fixture.GivenCategoryAsync();
        var product = await Fixture.GivenProductAsync(
            categoryId: productCategory.Id,
            slug: $"product-{TestToken}");

        var commandResult = ReslugProductCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            newSlug: sharedSlug);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.SlugNotAvailable>();
        error.Slug.Should().Be(sharedSlug);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.CurrentSlug.Should().HaveValue(product.CurrentSlug.Value);
        productAfter.RowVersion.Should().Be(product.RowVersion);

        var allSlugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.Slug == sharedSlug)
                .ToArrayAsync(ct));

        allSlugRegistrations.Should().ContainSingle(r =>
            r.Slug.Value == sharedSlug &&
            r.EntityType == EntityType.Category &&
            r.EntityId == category.Id &&
            r.IsActive);
    }

    [Fact]
    public async Task ReslugProduct_ShouldFail_WhenSlugBelongsToAnotherProduct()
    {
        var existingSlug = $"existing-{TestToken}";

        var category = await Fixture.GivenCategoryAsync();
        var existingProduct = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: existingSlug);

        var targetProduct = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: $"target-{TestToken}");

        var commandResult = ReslugProductCommand.Create(
            productId: targetProduct.Id,
            rowVersion: targetProduct.RowVersion,
            newSlug: existingSlug);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.SlugNotAvailable>();
        error.Slug.Should().Be(existingSlug);

        var targetAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == targetProduct.Id, ct));

        targetAfter.CurrentSlug.Should().HaveValue(targetProduct.CurrentSlug.Value);
        targetAfter.RowVersion.Should().Be(targetProduct.RowVersion);

        var targetRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Product && r.EntityId == targetProduct.Id)
                .ToArrayAsync(ct));

        targetRegistrations.Should().ContainSingle(r =>
            r.Slug.Value == targetProduct.CurrentSlug.Value && r.IsActive);

        var existingRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Product && r.EntityId == existingProduct.Id)
                .ToArrayAsync(ct));

        existingRegistrations.Should().ContainSingle(r =>
            r.Slug.Value == existingSlug && r.IsActive);
    }

    [Fact]
    public async Task ReslugProduct_ShouldFail_WhenRowVersionMismatch()
    {
        var category = await Fixture.GivenCategoryAsync();
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: $"concurrency-{TestToken}");

        var staleRowVersion = product.RowVersion + 1;

        var commandResult = ReslugProductCommand.Create(
            productId: product.Id,
            rowVersion: staleRowVersion,
            newSlug: $"other-{TestToken}");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.CurrentSlug.Should().HaveValue(product.CurrentSlug.Value);
        productAfter.RowVersion.Should().Be(product.RowVersion);
    }

    [Fact]
    public async Task ReslugProduct_ShouldFail_WhenProductMissing()
    {
        const long missingProductId = 999_999L;

        var commandResult = ReslugProductCommand.Create(
            productId: missingProductId,
            rowVersion: 1,
            newSlug: $"missing-{TestToken}");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundById>();
        error.Id.Should().Be(missingProductId);

        var exists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(p => p.Id == missingProductId, ct));

        exists.Should().BeFalse();
    }
}
