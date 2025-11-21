using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Categories.Commands.ArchiveCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Categories.Commands;

public sealed class ArchiveCategoryUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task ArchiveCategory_ShouldArchiveCategory_AndHideFromDefaultQueries()
    {
        var categoryBefore = await Fixture.GivenCategoryAsync();

        var archiveTimestamp = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);

        var commandResult = ArchiveCategoryCommand.Create(categoryBefore.Id, categoryBefore.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, archiveTimestamp, CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.RowVersion.Should().BeGreaterThan(categoryBefore.RowVersion);
        payload.DeletedAt.Should().Be(archiveTimestamp);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryBefore.Id, ct));

        categoryAfter.DeletedAt.Should().Be(archiveTimestamp);
        categoryAfter.RowVersion.Should().Be(payload.RowVersion);

        var isCategoryVisible = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .AnyAsync(c => c.Id == categoryBefore.Id, ct));

        isCategoryVisible.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveCategory_ShouldArchiveCategoryAndDescendants_WhenCategoryHasHierarchy()
    {
        var rootCategory = await Fixture.GivenCategoryAsync();

        var childCategory = await Fixture.GivenCategoryAsync(
            parentCategoryId: rootCategory.Id);

        var grandchildCategory = await Fixture.GivenCategoryAsync(
            parentCategoryId: childCategory.Id);

        var archiveTimestamp = new DateTimeOffset(2025, 7, 10, 16, 45, 0, TimeSpan.Zero);

        var commandResult = ArchiveCategoryCommand.Create(
            categoryId: rootCategory.Id,
            rowVersion: rootCategory.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, archiveTimestamp, CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.DeletedAt.Should().Be(archiveTimestamp);
        payload.RowVersion.Should().BeGreaterThan(rootCategory.RowVersion);

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

        var grandchildAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == grandchildCategory.Id, ct));

        rootAfter.DeletedAt.Should().Be(archiveTimestamp);
        rootAfter.RowVersion.Should().Be(payload.RowVersion);

        childAfter.DeletedAt.Should().Be(archiveTimestamp);
        childAfter.RowVersion.Should().BeGreaterThan(childCategory.RowVersion);

        grandchildAfter.DeletedAt.Should().Be(archiveTimestamp);
        grandchildAfter.RowVersion.Should().BeGreaterThan(grandchildCategory.RowVersion);
    }

    [Fact]
    public async Task ArchiveCategory_ShouldReturnExistingValues_WhenAlreadyArchived()
    {
        var archivalTimestamp = new DateTimeOffset(2025, 5, 20, 9, 15, 0, TimeSpan.Zero);

        var category = await Fixture.GivenCategoryAsync(
            isArchived: true,
            archivalTimestamp: archivalTimestamp);

        var commandResult = ArchiveCategoryCommand.Create(category.Id, category.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.DeletedAt.Should().Be(archivalTimestamp);
        payload.RowVersion.Should().Be(category.RowVersion);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.DeletedAt.Should().Be(archivalTimestamp);
        categoryAfter.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task ArchiveCategory_ShouldFail_WhenDescendantLimitExceeded()
    {
        var rootCategory = await Fixture.GivenCategoryAsync(
            name: $"Limited-{TestToken}",
            slug: $"limited-{TestToken}");

        for (var i = 1; i <= Category.DescendantLimitOnUpdate + 1; i++)
        {
            await Fixture.GivenCategoryAsync(
                name: $"Child-{TestToken}-{i}",
                slug: $"child-{TestToken}-{i}",
                parentCategoryId: rootCategory.Id);
        }

        var commandResult = ArchiveCategoryCommand.Create(
            categoryId: rootCategory.Id,
            rowVersion: rootCategory.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.TooManyDescendants>();
        error.CategoryId.Should().Be(rootCategory.Id);

        var rootAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == rootCategory.Id, ct));

        rootAfter.DeletedAt.Should().BeNull();
        rootAfter.RowVersion.Should().Be(rootCategory.RowVersion);
    }

    [Fact]
    public async Task ArchiveCategory_ShouldFail_WhenRowVersionMismatch()
    {
        var categoryBefore = await Fixture.GivenCategoryAsync();

        var staleRowVersion = categoryBefore.RowVersion + 1;

        var commandResult = ArchiveCategoryCommand.Create(categoryBefore.Id, staleRowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryBefore.Id, ct));

        categoryAfter.DeletedAt.Should().BeNull();
        categoryAfter.RowVersion.Should().Be(categoryBefore.RowVersion);
    }

    [Fact]
    public async Task ArchiveCategory_ShouldFail_WhenCategoryNotFound()
    {
        const long missingCategoryId = 999_999L;

        var commandResult = ArchiveCategoryCommand.Create(missingCategoryId, rowVersion: 1);
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
