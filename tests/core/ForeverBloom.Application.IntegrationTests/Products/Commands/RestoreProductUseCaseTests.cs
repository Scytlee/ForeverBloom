using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Products.Commands.RestoreProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Products.Commands;

public sealed class RestoreProductUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task RestoreProduct_ShouldRestoreArchivedProduct_AndMakeVisibleInDefaultQueries()
    {
        var category = await Fixture.GivenCategoryAsync();
        var productBefore = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);

        var commandResult = RestoreProductCommand.Create(productBefore.Id, productBefore.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().BeGreaterThan(productBefore.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.DeletedAt.Should().BeNull();
        productAfter.RowVersion.Should().Be(payload.RowVersion);

        var isProductVisible = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .AnyAsync(p => p.Id == productBefore.Id, ct));

        isProductVisible.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreProduct_ShouldReturnExistingValues_WhenProductAlreadyRestored()
    {
        var category = await Fixture.GivenCategoryAsync();
        var productBefore = await Fixture.GivenProductAsync(categoryId: category.Id);

        var commandResult = RestoreProductCommand.Create(productBefore.Id, productBefore.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().Be(productBefore.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.DeletedAt.Should().BeNull();
        productAfter.RowVersion.Should().Be(productBefore.RowVersion);
    }

    [Fact]
    public async Task RestoreProduct_ShouldFail_WhenRowVersionMismatch()
    {
        var category = await Fixture.GivenCategoryAsync();
        var productBefore = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);

        var staleRowVersion = productBefore.RowVersion + 1;

        var commandResult = RestoreProductCommand.Create(productBefore.Id, staleRowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.DeletedAt.Should().Be(productBefore.DeletedAt);
        productAfter.RowVersion.Should().Be(productBefore.RowVersion);
    }

    [Fact]
    public async Task RestoreProduct_ShouldFail_WhenProductNotFound()
    {
        const long missingProductId = 999_999L;

        var commandResult = RestoreProductCommand.Create(missingProductId, rowVersion: 1);
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
