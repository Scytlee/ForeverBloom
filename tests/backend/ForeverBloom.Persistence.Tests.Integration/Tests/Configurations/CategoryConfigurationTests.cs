using FluentAssertions;
using ForeverBloom.Persistence.Entities;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Seeding.Database;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Tests.Configurations;

public sealed class CategoryConfigurationTests : DatabaseTestClassBase
{
    public CategoryConfigurationTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task CategoryTable_ShouldEnforceNameUniquenessWithinParent()
    {
        // Seed one parent and one child category
        var parentCategory = await DbContext.CreateCategoryAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await DbContext.CreateCategoryAsync(
            name: "Child",
            parentCategoryId: parentCategory.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var childWithDuplicateName = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving(
            name: "Child",
            parentCategoryId: parentCategory.Id);
        DbContext.Categories.Add(childWithDuplicateName);
        var act = async () => await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task CategoryTable_ShouldRestrictDeletingParentWithChildren()
    {
        var parentCategory = await DbContext.CreateCategoryAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await DbContext.CreateCategoryAsync(
            parentCategoryId: parentCategory.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var categoryToDelete = new Category { Id = parentCategory.Id, RowVersion = parentCategory.RowVersion };
        DbContext.Categories.Remove(categoryToDelete);
        var act = async () => await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task CategoryTable_ShouldRestrictDeletingCategoryWithProducts()
    {
        var category = await DbContext.CreateCategoryAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await DbContext.CreateProductAsync(
            categoryId: category.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        var categoryToDelete = new Category { Id = category.Id, RowVersion = category.RowVersion };
        DbContext.Categories.Remove(categoryToDelete);
        var act = async () => await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
