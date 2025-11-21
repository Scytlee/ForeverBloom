using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Categories.Commands.RestoreCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Categories.Commands;

public sealed class RestoreCategoryUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task RestoreCategory_ShouldRestoreCategoryAndDescendants_AndMakeVisibleInDefaultQueries()
    {
        var rootCategory = await Fixture.GivenCategoryAsync();

        var childCategory = await Fixture.GivenCategoryAsync(
            parentCategoryId: rootCategory.Id);

        var grandchildCategory = await Fixture.GivenCategoryAsync(
            parentCategoryId: childCategory.Id);

        var archivalTimestamp = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);
        rootCategory = await Fixture.ArchiveExistingCategoryAsync(rootCategory, archivalTimestamp);

        var restoreTimestamp = new DateTimeOffset(2025, 6, 16, 10, 0, 0, TimeSpan.Zero);

        var commandResult = RestoreCategoryCommand.Create(
            rootCategory.Id,
            rootCategory.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, restoreTimestamp, CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().BeGreaterThan(rootCategory.RowVersion);

        var rootAfterRestore = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == rootCategory.Id, ct));

        var childAfterRestore = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == childCategory.Id, ct));

        var grandchildAfterRestore = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == grandchildCategory.Id, ct));

        rootAfterRestore.DeletedAt.Should().BeNull();
        rootAfterRestore.RowVersion.Should().Be(payload.RowVersion);

        childAfterRestore.DeletedAt.Should().BeNull();
        childAfterRestore.RowVersion.Should().BeGreaterThan(childCategory.RowVersion);

        grandchildAfterRestore.DeletedAt.Should().BeNull();
        grandchildAfterRestore.RowVersion.Should().BeGreaterThan(grandchildCategory.RowVersion);

        var isRootVisible = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .AnyAsync(c => c.Id == rootCategory.Id, ct));

        isRootVisible.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreCategory_ShouldReturnExistingValues_WhenCategoryAlreadyRestored()
    {
        var category = await Fixture.GivenCategoryAsync();

        var commandResult = RestoreCategoryCommand.Create(category.Id, category.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().Be(category.RowVersion);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.DeletedAt.Should().BeNull();
        categoryAfter.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task RestoreCategory_ShouldFail_WhenAncestorStillArchived()
    {
        var rootCategory = await Fixture.GivenCategoryAsync();

        var childCategory = await Fixture.GivenCategoryAsync(
            parentCategoryId: rootCategory.Id);

        var archivalTimestamp = new DateTimeOffset(2025, 7, 10, 16, 45, 0, TimeSpan.Zero);
        // Archive root and reload child to get the row version
        rootCategory = await Fixture.ArchiveExistingCategoryAsync(rootCategory, archivalTimestamp);
        await ExecuteDbContextAsync((db, ct) => db.Entry(childCategory).ReloadAsync(ct));

        var commandResult = RestoreCategoryCommand.Create(
            childCategory.Id,
            childCategory.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.HasArchivedAncestors>();
        error.CategoryId.Should().Be(childCategory.Id);

        var rootAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == rootCategory.Id, ct));

        var childAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == childCategory.Id, ct));

        rootAfter.DeletedAt.Should().Be(archivalTimestamp);
        childAfter.DeletedAt.Should().Be(archivalTimestamp);
    }

    [Fact]
    public async Task RestoreCategory_ShouldFail_WhenRowVersionMismatch()
    {
        var archivalTimestamp = new DateTimeOffset(2025, 5, 20, 9, 15, 0, TimeSpan.Zero);

        var category = await Fixture.GivenCategoryAsync(
            isArchived: true,
            archivalTimestamp: archivalTimestamp);

        var staleRowVersion = category.RowVersion + 1;

        var commandResult = RestoreCategoryCommand.Create(category.Id, staleRowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.DeletedAt.Should().Be(archivalTimestamp);
        categoryAfter.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task RestoreCategory_ShouldFail_WhenCategoryNotFound()
    {
        const long missingCategoryId = 999_999L;

        var commandResult = RestoreCategoryCommand.Create(missingCategoryId, rowVersion: 1);
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
}
