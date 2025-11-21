using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Products.Commands.ArchiveProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Products.Commands;

public sealed class ArchiveProductUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task ArchiveProduct_ShouldArchiveProduct_AndHideFromDefaultQueries()
    {
        var category = await Fixture.GivenCategoryAsync();
        var productBefore = await Fixture.GivenProductAsync(categoryId: category.Id);

        var archiveTimestamp = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);

        var commandResult = ArchiveProductCommand.Create(productBefore.Id, productBefore.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, archiveTimestamp, CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.RowVersion.Should().BeGreaterThan(productBefore.RowVersion);
        payload.DeletedAt.Should().Be(archiveTimestamp);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.DeletedAt.Should().Be(archiveTimestamp);
        productAfter.RowVersion.Should().Be(payload.RowVersion);

        var isProductVisible = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .AnyAsync(p => p.Id == productBefore.Id, ct));

        isProductVisible.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveProduct_ShouldReturnExistingValues_WhenAlreadyArchived()
    {
        var category = await Fixture.GivenCategoryAsync();
        var archivalTimestamp = Fixture.TimeProvider.CurrentTime.AddDays(1);
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true,
            archivalTimestamp: archivalTimestamp);

        var commandResult = ArchiveProductCommand.Create(product.Id, product.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.DeletedAt.Should().Be(archivalTimestamp);
        payload.RowVersion.Should().Be(product.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.DeletedAt.Should().Be(archivalTimestamp);
        productAfter.RowVersion.Should().Be(product.RowVersion);
    }

    [Fact]
    public async Task ArchiveProduct_ShouldFail_WhenRowVersionMismatch()
    {
        var category = await Fixture.GivenCategoryAsync();
        var productBefore = await Fixture.GivenProductAsync(categoryId: category.Id);

        var staleRowVersion = productBefore.RowVersion + 1;

        var commandResult = ArchiveProductCommand.Create(productBefore.Id, staleRowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.DeletedAt.Should().BeNull();
        productAfter.RowVersion.Should().Be(productBefore.RowVersion);
    }

    [Fact]
    public async Task ArchiveProduct_ShouldFail_WhenProductNotFound()
    {
        const long missingProductId = 999_999L;

        var commandResult = ArchiveProductCommand.Create(missingProductId, rowVersion: 1);
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
