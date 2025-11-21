using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Categories.Commands.DeleteCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.SlugRegistry;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Categories.Commands;

public sealed class DeleteCategoryUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task DeleteCategory_ShouldPermanentlyRemoveCategoryAndAllSlugs_WhenGracePeriodElapsed()
    {
        // Deletion will occur 1 hour after grace period ends
        var archivalTimestamp = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var deletionTimestamp = archivalTimestamp.AddHours(Category.DeletionGracePeriodInHours + 1);

        var category = await Fixture.GivenCategoryAsync(
            slug: $"permanent-{TestToken}",
            slugHistory: [$"permanent-{TestToken}-old1", $"permanent-{TestToken}-old2"],
            isArchived: true,
            archivalTimestamp: archivalTimestamp);

        var commandResult = DeleteCategoryCommand.Create(category.Id, category.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, deletionTimestamp, CancellationToken);

        result.Should().BeSuccess();

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == category.Id, ct));
        stillExists.Should().BeFalse();

        var slugRegistrations = await GetSlugRegistrationsAsync(category.Id);
        slugRegistrations.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCategory_ShouldFail_WhenCategoryNotFound()
    {
        const long missingCategoryId = 999_999L;

        var commandResult = DeleteCategoryCommand.Create(missingCategoryId, rowVersion: 1);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NotFoundById>();
        error.Id.Should().Be(missingCategoryId);

        var exists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(c => c.Id == missingCategoryId, ct));
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCategory_ShouldFail_WhenRowVersionMismatch()
    {
        var archivalTimestamp = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var deletionTimestamp = archivalTimestamp.AddHours(Category.DeletionGracePeriodInHours + 1);

        var category = await Fixture.GivenCategoryAsync(
            isArchived: true,
            archivalTimestamp: archivalTimestamp);

        var staleRowVersion = category.RowVersion + 1;

        var commandResult = DeleteCategoryCommand.Create(category.Id, staleRowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, deletionTimestamp, CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == category.Id, ct));
        stillExists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCategory_ShouldFail_WhenCategoryNotArchived()
    {
        var category = await Fixture.GivenCategoryAsync();

        var commandResult = DeleteCategoryCommand.Create(category.Id, category.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.CannotDeleteNotArchived>();
        error.CategoryId.Should().Be(category.Id);

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .AnyAsync(c => c.Id == category.Id, ct));
        stillExists.Should().BeTrue();

        var slugRegistrations = await GetSlugRegistrationsAsync(category.Id);
        slugRegistrations.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteCategory_ShouldFail_WhenGracePeriodNotElapsed()
    {
        // Deletion will occur 1 hour BEFORE grace period ends
        var archivalTimestamp = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var tooEarlyDeletionTimestamp = archivalTimestamp.AddHours(Category.DeletionGracePeriodInHours - 1);

        var category = await Fixture.GivenCategoryAsync(
            isArchived: true,
            archivalTimestamp: archivalTimestamp);

        var commandResult = DeleteCategoryCommand.Create(category.Id, category.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, tooEarlyDeletionTimestamp, CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.CannotDeleteTooSoon>();
        error.CategoryId.Should().Be(category.Id);
        error.ArchivedAt.Should().Be(archivalTimestamp);
        error.EligibleAt.Should().Be(archivalTimestamp.AddHours(Category.DeletionGracePeriodInHours));

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(c => c.Id == category.Id, ct));
        stillExists.Should().BeTrue();

        var slugRegistrations = await GetSlugRegistrationsAsync(category.Id);
        slugRegistrations.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteCategory_ShouldFail_WhenHasChildren()
    {
        var archivalTimestamp = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var deletionTimestamp = archivalTimestamp.AddHours(Category.DeletionGracePeriodInHours + 1);

        var rootCategory = await Fixture.GivenCategoryAsync();

        await Fixture.GivenCategoryAsync(
            parentCategoryId: rootCategory.Id);

        rootCategory = await Fixture.ArchiveExistingCategoryAsync(rootCategory, archivalTimestamp);

        var commandResult = DeleteCategoryCommand.Create(rootCategory.Id, rootCategory.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, deletionTimestamp, CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.CannotDeleteHasChildren>();
        error.CategoryId.Should().Be(rootCategory.Id);

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == rootCategory.Id, ct));
        stillExists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCategory_ShouldFail_WhenHasProducts()
    {
        var archivalTimestamp = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var deletionTimestamp = archivalTimestamp.AddHours(Category.DeletionGracePeriodInHours + 1);

        var category = await Fixture.GivenCategoryAsync();

        await Fixture.GivenProductAsync(categoryId: category.Id);

        category = await Fixture.ArchiveExistingCategoryAsync(category, archivalTimestamp);

        var commandResult = DeleteCategoryCommand.Create(category.Id, category.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, deletionTimestamp, CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.CannotDeleteHasProducts>();
        error.CategoryId.Should().Be(category.Id);

        var stillExists = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == category.Id, ct));
        stillExists.Should().BeTrue();
    }

    private Task<IReadOnlyList<SlugRegistration>> GetSlugRegistrationsAsync(long categoryId)
    {
        return ExecuteDbContextAsync(
            async (db, ct) =>
            {
                var registrations = await db.Set<SlugRegistration>()
                    .AsNoTracking()
                    .Where(r => r.EntityType == EntityType.Category && r.EntityId == categoryId)
                    .ToListAsync(ct);

                return (IReadOnlyList<SlugRegistration>)registrations;
            });
    }
}
