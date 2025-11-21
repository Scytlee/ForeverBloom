using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Products.Commands.DeleteProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.SlugRegistry;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Products.Commands;

public sealed class DeleteProductUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task DeleteProduct_ShouldPermanentlyRemoveProductAndAllSlugs_WhenGracePeriodElapsed()
    {
        var category = await Fixture.GivenCategoryAsync();
        var archivalTimestamp = Fixture.TimeProvider.CurrentTime.AddDays(1);
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: $"permanent-{TestToken}",
            slugHistory: [$"permanent-{TestToken}-old1", $"permanent-{TestToken}-old2"],
            isArchived: true,
            archivalTimestamp: archivalTimestamp);

        var commandResult = DeleteProductCommand.Create(product.Id, product.RowVersion);
        commandResult.Should().BeSuccess();

        // Deletion will occur 1 hour after grace period ends
        Fixture.TimeProvider.FastForwardBy(TimeSpan.FromHours(Product.DeletionGracePeriodInHours + 1));
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Id == product.Id, ct));
        stillExists.Should().BeFalse();

        var slugRegistrations = await GetSlugRegistrationsAsync(product.Id);
        slugRegistrations.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteProduct_ShouldFail_WhenProductNotFound()
    {
        const long missingProductId = 999_999L;

        var commandResult = DeleteProductCommand.Create(missingProductId, rowVersion: 1);
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

    [Fact]
    public async Task DeleteProduct_ShouldFail_WhenRowVersionMismatch()
    {
        var category = await Fixture.GivenCategoryAsync();
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);

        var staleRowVersion = product.RowVersion + 1;

        var commandResult = DeleteProductCommand.Create(product.Id, staleRowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Id == product.Id, ct));
        stillExists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProduct_ShouldFail_WhenProductNotArchived()
    {
        var category = await Fixture.GivenCategoryAsync();
        var product = await Fixture.GivenProductAsync(categoryId: category.Id);

        var commandResult = DeleteProductCommand.Create(product.Id, product.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.CannotDeleteNotArchived>();
        error.ProductId.Should().Be(product.Id);

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .AnyAsync(p => p.Id == product.Id, ct));
        stillExists.Should().BeTrue();

        var slugRegistrations = await GetSlugRegistrationsAsync(product.Id);
        slugRegistrations.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteProduct_ShouldFail_WhenGracePeriodNotElapsed()
    {
        var category = await Fixture.GivenCategoryAsync();
        var archivalTimestamp = Fixture.TimeProvider.CurrentTime.AddDays(1);
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true,
            archivalTimestamp: archivalTimestamp);

        var commandResult = DeleteProductCommand.Create(product.Id, product.RowVersion);
        commandResult.Should().BeSuccess();

        // Deletion will occur 1 hour BEFORE grace period ends
        Fixture.TimeProvider.FastForwardBy(TimeSpan.FromHours(Product.DeletionGracePeriodInHours - 1));
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.CannotDeleteTooSoon>();
        error.ProductId.Should().Be(product.Id);
        error.ArchivedAt.Should().Be(archivalTimestamp);
        error.EligibleAt.Should().Be(archivalTimestamp.AddHours(Product.DeletionGracePeriodInHours));

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(p => p.Id == product.Id, ct));
        stillExists.Should().BeTrue();

        var slugRegistrations = await GetSlugRegistrationsAsync(product.Id);
        slugRegistrations.Should().HaveCount(1);
    }

    private Task<IReadOnlyList<SlugRegistration>> GetSlugRegistrationsAsync(long productId)
    {
        return ExecuteDbContextAsync(
            async (db, ct) =>
            {
                var registrations = await db.Set<SlugRegistration>()
                    .AsNoTracking()
                    .Where(r => r.EntityType == EntityType.Product && r.EntityId == productId)
                    .ToListAsync(ct);

                return (IReadOnlyList<SlugRegistration>)registrations;
            });
    }
}
