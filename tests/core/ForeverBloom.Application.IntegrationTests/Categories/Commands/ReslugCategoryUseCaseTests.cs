using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Categories.Commands.ReslugCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.SlugRegistry;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Categories.Commands;

public sealed class ReslugCategoryUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task ReslugCategory_ShouldUpdateSlug_WhenCategoryIsRootAndHasNoDescendants()
    {
        var originalSlug = $"original-{TestToken}";
        var newSlug = $"updated-{TestToken}";

        var category = await Fixture.GivenCategoryAsync(
            slug: originalSlug);

        var commandResult = ReslugCategoryCommand.Create(
            categoryId: category.Id,
            rowVersion: category.RowVersion,
            newSlug: newSlug);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Slug.Should().Be(newSlug);
        payload.Path.Should().Be(newSlug);
        payload.RowVersion.Should().BeGreaterThan(category.RowVersion);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.CurrentSlug.Should().HaveValue(newSlug);
        categoryAfter.Path.Should().HaveValue(newSlug);
        categoryAfter.RowVersion.Should().Be(payload.RowVersion);
        categoryAfter.UpdatedAt.Should().Be(payload.UpdatedAt);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Category && r.EntityId == category.Id)
                .OrderBy(r => r.Id)
                .ToArrayAsync(ct));

        slugRegistrations.Should().HaveCount(2);
        slugRegistrations.Should().Contain(r => r.Slug.Value == originalSlug && !r.IsActive);
        slugRegistrations.Should().Contain(r => r.Slug.Value == newSlug && r.IsActive);
    }

    [Fact]
    public async Task ReslugCategory_ShouldUpdateSlugAndRebaseDescendants_WhenSlugChanges()
    {
        var originalRootSlug = $"root-{TestToken}";
        var newRootSlug = $"updated-root-{TestToken}";

        var root = await Fixture.GivenCategoryAsync(
            slug: originalRootSlug);

        var child = await Fixture.GivenCategoryAsync(
            parentCategoryId: root.Id);

        var grandchild = await Fixture.GivenCategoryAsync(
            parentCategoryId: child.Id);

        var commandResult = ReslugCategoryCommand.Create(
            categoryId: root.Id,
            rowVersion: root.RowVersion,
            newSlug: newRootSlug);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Slug.Should().Be(newRootSlug);
        payload.Path.Should().Be(newRootSlug);
        payload.RowVersion.Should().BeGreaterThan(root.RowVersion);

        var categoriesAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.Id == root.Id || c.Id == child.Id || c.Id == grandchild.Id)
                .ToArrayAsync(ct));

        var rootAfter = categoriesAfter.Single(c => c.Id == root.Id);
        rootAfter.CurrentSlug.Should().HaveValue(newRootSlug);
        rootAfter.Path.Should().HaveValue(newRootSlug);
        rootAfter.RowVersion.Should().Be(payload.RowVersion);

        var childSuffix = child.Path.Value[root.Path.Value.Length..];
        var grandchildSuffix = grandchild.Path.Value[root.Path.Value.Length..];

        var childAfter = categoriesAfter.Single(c => c.Id == child.Id);
        childAfter.Path.Should().HaveValue(payload.Path + childSuffix);
        childAfter.CurrentSlug.Should().HaveValue(child.CurrentSlug.Value);

        var grandchildAfter = categoriesAfter.Single(c => c.Id == grandchild.Id);
        grandchildAfter.Path.Should().HaveValue(payload.Path + grandchildSuffix);
        grandchildAfter.CurrentSlug.Should().HaveValue(grandchild.CurrentSlug.Value);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Category && r.EntityId == root.Id)
                .OrderBy(r => r.Id)
                .ToArrayAsync(ct));

        slugRegistrations.Should().Contain(r => r.Slug.Value == newRootSlug && r.IsActive);
        slugRegistrations.Should().Contain(r => r.Slug.Value == originalRootSlug && !r.IsActive);
    }

    [Fact]
    public async Task ReslugCategory_ShouldReactivateHistoricalSlug_WhenReverting()
    {
        var originalSlug = $"initial-{TestToken}";
        var temporarySlug = $"temporary-{TestToken}";

        var category = await Fixture.GivenCategoryAsync(
            slug: temporarySlug,
            slugHistory: [originalSlug]);

        var commandResult = ReslugCategoryCommand.Create(
            categoryId: category.Id,
            rowVersion: category.RowVersion,
            newSlug: originalSlug);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Slug.Should().Be(originalSlug);
        payload.RowVersion.Should().BeGreaterThan(category.RowVersion);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.CurrentSlug.Should().HaveValue(originalSlug);
        categoryAfter.Path.Should().HaveValue(originalSlug);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Category && r.EntityId == category.Id)
                .OrderBy(r => r.Id)
                .ToArrayAsync(ct));

        slugRegistrations.Should().HaveCount(2);
        slugRegistrations.Should().Contain(r => r.Slug.Value == originalSlug && r.IsActive);
        slugRegistrations.Should().Contain(r => r.Slug.Value == temporarySlug && !r.IsActive);
    }

    [Fact]
    public async Task ReslugCategory_ShouldReturnExistingValues_WhenSlugUnchanged()
    {
        var category = await Fixture.GivenCategoryAsync();

        var commandResult = ReslugCategoryCommand.Create(
            categoryId: category.Id,
            rowVersion: category.RowVersion,
            newSlug: category.CurrentSlug.Value);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Slug.Should().Be(category.CurrentSlug.Value);
        payload.RowVersion.Should().Be(category.RowVersion);
        payload.UpdatedAt.Should().Be(category.UpdatedAt);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.RowVersion.Should().Be(category.RowVersion);
        categoryAfter.CurrentSlug.Should().HaveValue(category.CurrentSlug.Value);
        categoryAfter.Path.Should().HaveValue(category.Path.Value);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Category && r.EntityId == category.Id)
                .ToArrayAsync(ct));

        slugRegistrations.Should().ContainSingle();
        slugRegistrations.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ReslugCategory_ShouldFail_WhenSlugBelongsToADifferentEntityType()
    {
        var sharedSlug = $"shared-{TestToken}";

        var category1 = await Fixture.GivenCategoryAsync();
        var product1 = await Fixture.GivenProductAsync(
            categoryId: category1.Id,
            slug: sharedSlug);

        var category2 = await Fixture.GivenCategoryAsync(
            slug: $"category-{TestToken}");

        var commandResult = ReslugCategoryCommand.Create(
            categoryId: category2.Id,
            rowVersion: category2.RowVersion,
            newSlug: sharedSlug);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.SlugNotAvailable>();
        error.Slug.Should().Be(sharedSlug);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category2.Id, ct));

        categoryAfter.CurrentSlug.Should().HaveValue(category2.CurrentSlug.Value);
        categoryAfter.Path.Should().HaveValue(category2.Path.Value);
        categoryAfter.RowVersion.Should().Be(category2.RowVersion);

        var allSlugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.Slug == sharedSlug)
                .ToArrayAsync(ct));

        allSlugRegistrations.Should().ContainSingle(r =>
            r.Slug.Value == sharedSlug &&
            r.EntityType == EntityType.Product &&
            r.EntityId == product1.Id &&
            r.IsActive);
    }

    [Fact]
    public async Task ReslugCategory_ShouldFail_WhenSlugBelongsToAnotherCategory()
    {
        var existingSlug = $"existing-{TestToken}";

        var existingCategory = await Fixture.GivenCategoryAsync(
            slug: existingSlug);

        var targetCategory = await Fixture.GivenCategoryAsync(
            slug: $"target-{TestToken}");

        var commandResult = ReslugCategoryCommand.Create(
            categoryId: targetCategory.Id,
            rowVersion: targetCategory.RowVersion,
            newSlug: existingSlug);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.SlugNotAvailable>();
        error.Slug.Should().Be(existingSlug);

        var targetAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == targetCategory.Id, ct));

        targetAfter.CurrentSlug.Should().HaveValue(targetCategory.CurrentSlug.Value);
        targetAfter.Path.Should().HaveValue(targetCategory.Path.Value);
        targetAfter.RowVersion.Should().Be(targetCategory.RowVersion);

        var targetRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Category && r.EntityId == targetCategory.Id)
                .ToArrayAsync(ct));

        targetRegistrations.Should().ContainSingle(r =>
            r.Slug.Value == targetCategory.CurrentSlug.Value && r.IsActive);

        var existingRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Category && r.EntityId == existingCategory.Id)
                .ToArrayAsync(ct));

        existingRegistrations.Should().ContainSingle(r =>
            r.Slug.Value == existingSlug && r.IsActive);
    }

    [Fact]
    public async Task ReslugCategory_ShouldFail_WhenDescendantLimitExceeded()
    {
        var originalSlug = $"limited-{TestToken}";

        var rootCategory = await Fixture.GivenCategoryAsync(
            slug: originalSlug);

        for (var i = 1; i <= Category.DescendantLimitOnUpdate + 1; i++)
        {
            await Fixture.GivenCategoryAsync(
                name: $"Child-{i}-{TestToken}",
                slug: $"child-{i}-{TestToken}",
                parentCategoryId: rootCategory.Id);
        }

        var commandResult = ReslugCategoryCommand.Create(
            categoryId: rootCategory.Id,
            rowVersion: rootCategory.RowVersion,
            newSlug: $"limited-updated-{TestToken}");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.TooManyDescendants>();
        error.CategoryId.Should().Be(rootCategory.Id);

        var rootAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == rootCategory.Id, ct));

        rootAfter.CurrentSlug.Should().HaveValue(originalSlug);
        rootAfter.Path.Should().HaveValue(originalSlug);
        rootAfter.RowVersion.Should().Be(rootCategory.RowVersion);
    }

    [Fact]
    public async Task ReslugCategory_ShouldFail_WhenRowVersionMismatch()
    {
        var category = await Fixture.GivenCategoryAsync(
            name: $"Concurrency-{TestToken}",
            slug: $"concurrency-{TestToken}");

        var staleRowVersion = category.RowVersion + 1;

        var commandResult = ReslugCategoryCommand.Create(
            categoryId: category.Id,
            rowVersion: staleRowVersion,
            newSlug: $"other-{TestToken}");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.CurrentSlug.Should().HaveValue(category.CurrentSlug.Value);
        categoryAfter.Path.Should().HaveValue(category.Path.Value);
        categoryAfter.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task ReslugCategory_ShouldFail_WhenCategoryMissing()
    {
        const long missingCategoryId = 999_999L;

        var commandResult = ReslugCategoryCommand.Create(
            categoryId: missingCategoryId,
            rowVersion: 1,
            newSlug: $"missing-{TestToken}");
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
