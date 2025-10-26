using FluentAssertions;
using ForeverBloom.Persistence.Repositories;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Seeding.Database;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Tests.Repositories;

public sealed class CategoryRepositoryTests : DatabaseTestClassBase
{
    public CategoryRepositoryTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task InsertCategory_ShouldPersistCategoryInDatabase()
    {
        var sut = new CategoryRepository(DbContext, TimeProvider.System);
        var category = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving(
            name: $"Test Category {_testId:N}"[..20],
            slug: $"test-category-{_testId:N}"[..20]);

        sut.InsertCategory(category);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var categoryInDatabase = await DbContext.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken);
        categoryInDatabase.Should().NotBeNull();

        AssertionHelpers.AssertAllPropertiesAreMapped(
            sourceObject: category,
            destinationObject: categoryInDatabase,
            sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(category.ParentCategory),
                nameof(category.ChildCategories),
                nameof(category.Products)
            },
            equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));
    }

    [Fact]
    public async Task UpdateCategory_ShouldUpdateCategoryInDatabase()
    {
        var sut = new CategoryRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(
            name: $"Original Category {_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);
        var originalRowVersion = category.RowVersion;

        // Modify category properties
        category.Name = $"Updated Category {_testId:N}"[..20];
        category.Description = "Updated description";
        category.DisplayOrder = 5;
        category.IsActive = false;

        DbContext.Attach(category);
        sut.UpdateCategory(category);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var categoryInDatabase = await DbContext.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken);
        categoryInDatabase.Should().NotBeNull();

        AssertionHelpers.AssertAllPropertiesAreMapped(
            sourceObject: category,
            destinationObject: categoryInDatabase,
            sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(category.ParentCategory),
                nameof(category.ChildCategories),
                nameof(category.Products)
            },
            equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        categoryInDatabase.RowVersion.Should().NotBe(originalRowVersion);
        categoryInDatabase.UpdatedAt.Should().BeAfter(categoryInDatabase.CreatedAt);
    }

    [Fact]
    public async Task ArchiveCategory_ShouldSetDeletedAtInDatabase()
    {
        var sut = new CategoryRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var originalRowVersion = category.RowVersion;

        var lowerBound = TimeProvider.System.GetUtcNow();
        DbContext.Attach(category);
        sut.ArchiveCategory(category);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var upperBound = TimeProvider.System.GetUtcNow();

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var categoryInDatabase = await DbContext.Categories
            .IgnoreQueryFilters()
            .SingleAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);
        categoryInDatabase.Should().NotBeNull();
        categoryInDatabase.UpdatedAt.Should().BeAfter(categoryInDatabase.CreatedAt);
        categoryInDatabase.UpdatedAt.Should().BeOnOrAfter(lowerBound);
        categoryInDatabase.UpdatedAt.Should().BeOnOrBefore(upperBound);
        categoryInDatabase.DeletedAt.Should().NotBeNull();
        categoryInDatabase.DeletedAt.Should().BeOnOrAfter(lowerBound);
        categoryInDatabase.DeletedAt.Should().BeOnOrBefore(upperBound);
        categoryInDatabase.RowVersion.Should().NotBe(originalRowVersion);

        // Verify category is filtered out by default query
        DbContext.ChangeTracker.Clear();
        var categoryViaFilteredQuery = await DbContext.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken);
        categoryViaFilteredQuery.Should().BeNull();
    }

    [Fact]
    public async Task RestoreCategory_ShouldClearDeletedAtInDatabase()
    {
        var sut = new CategoryRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        DbContext.Attach(category);
        sut.ArchiveCategory(category);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var originalUpdatedAt = category.UpdatedAt;
        var originalRowVersion = category.RowVersion;

        // Then restore it
        var lowerBound = TimeProvider.System.GetUtcNow();
        sut.RestoreCategory(category);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var upperBound = TimeProvider.System.GetUtcNow();

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var categoryInDatabase = await DbContext.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken);
        categoryInDatabase.Should().NotBeNull();
        categoryInDatabase.UpdatedAt.Should().NotBe(originalUpdatedAt);
        categoryInDatabase.UpdatedAt.Should().BeOnOrAfter(lowerBound);
        categoryInDatabase.UpdatedAt.Should().BeOnOrBefore(upperBound);
        categoryInDatabase.DeletedAt.Should().BeNull();
        categoryInDatabase.RowVersion.Should().NotBe(originalRowVersion);

        // Verify category is once again queryable via default query
        DbContext.ChangeTracker.Clear();
        var categoryViaFilteredQuery = await DbContext.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken);
        categoryViaFilteredQuery.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCategory_ShouldRemoveCategoryFromDatabase()
    {
        var sut = new CategoryRepository(DbContext, TimeProvider.System);
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var categoryId = category.Id;
        var rowVersion = category.RowVersion;
        DbContext.ChangeTracker.Clear();

        sut.DeleteCategory(categoryId, rowVersion);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify category is completely removed from database
        var categoryInDatabase = await DbContext.Categories
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.Id == categoryId, TestContext.Current.CancellationToken);
        categoryInDatabase.Should().BeNull();
    }
}
