using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Categories.Commands.ReparentCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Categories.Commands;

public sealed class ReparentCategoryUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task ReparentCategory_ShouldMoveRootToChild_WhenNewParentExistsAndNoDescendants()
    {
        var parentCategory = await Fixture.GivenCategoryAsync(
            slug: $"parent-{TestToken}");

        var rootCategory = await Fixture.GivenCategoryAsync(
            slug: $"root-{TestToken}");

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: rootCategory.Id,
            rowVersion: rootCategory.RowVersion,
            newParentCategoryId: parentCategory.Id);
        commandResult.Should().BeSuccess();

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Path.Should().Be($"{parentCategory.Path.Value}.{rootCategory.CurrentSlug.Value}");
        payload.ParentCategoryId.Should().Be(parentCategory.Id);
        payload.RowVersion.Should().BeGreaterThan(rootCategory.RowVersion);
        payload.UpdatedAt.Should().Be(actionTimestamp);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == rootCategory.Id, ct));

        categoryAfter.ParentCategoryId.Should().Be(parentCategory.Id);
        categoryAfter.Path.Should().HaveValue($"{parentCategory.Path.Value}.{rootCategory.CurrentSlug.Value}");
        categoryAfter.RowVersion.Should().Be(payload.RowVersion);
        categoryAfter.UpdatedAt.Should().Be(payload.UpdatedAt);
    }

    [Fact]
    public async Task ReparentCategory_ShouldReparentAndRebaseDescendants_WhenCategoryHasDescendants()
    {
        var root = await Fixture.GivenCategoryAsync(
            slug: $"root-{TestToken}");

        var categoryToMove = await Fixture.GivenCategoryAsync(
            slug: $"to-move-{TestToken}",
            parentCategoryId: root.Id);

        var child = await Fixture.GivenCategoryAsync(
            slug: $"child-{TestToken}",
            parentCategoryId: categoryToMove.Id);

        var grandchild = await Fixture.GivenCategoryAsync(
            slug: $"grandchild-{TestToken}",
            parentCategoryId: child.Id);

        var newParent = await Fixture.GivenCategoryAsync(
            slug: $"new-parent-{TestToken}");

        // Arranged hierarchy:
        // - root -> categoryToMove -> child -> grandchild
        // - newParent

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: categoryToMove.Id,
            rowVersion: categoryToMove.RowVersion,
            newParentCategoryId: newParent.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        // New hierarchy:
        // - root
        // - newParent -> categoryToMove -> child -> grandchild

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Path.Should().Be($"{newParent.Path.Value}.{categoryToMove.CurrentSlug.Value}");
        payload.ParentCategoryId.Should().Be(newParent.Id);
        payload.RowVersion.Should().BeGreaterThan(categoryToMove.RowVersion);

        var categoriesAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.Id == categoryToMove.Id || c.Id == child.Id || c.Id == grandchild.Id)
                .ToArrayAsync(ct));

        var categoryToMoveAfter = categoriesAfter.Single(c => c.Id == categoryToMove.Id);
        categoryToMoveAfter.Path.Should().HaveValue($"{newParent.Path.Value}.{categoryToMove.CurrentSlug.Value}");
        categoryToMoveAfter.ParentCategoryId.Should().Be(newParent.Id);
        categoryToMoveAfter.RowVersion.Should().Be(payload.RowVersion);

        var childAfter = categoriesAfter.Single(c => c.Id == child.Id);
        childAfter.Path.Should().HaveValue($"{categoryToMoveAfter.Path.Value}.{childAfter.CurrentSlug.Value}");
        childAfter.ParentCategoryId.Should().Be(categoryToMove.Id);
        childAfter.RowVersion.Should().BeGreaterThan(child.RowVersion);

        var grandchildAfter = categoriesAfter.Single(c => c.Id == grandchild.Id);
        grandchildAfter.Path.Should().HaveValue($"{childAfter.Path.Value}.{grandchildAfter.CurrentSlug.Value}");
        grandchildAfter.ParentCategoryId.Should().Be(child.Id);
        grandchildAfter.RowVersion.Should().BeGreaterThan(grandchild.RowVersion);
    }

    [Fact]
    public async Task ReparentCategory_ShouldMoveChildToRoot_WhenNewParentIsNull()
    {
        var parent = await Fixture.GivenCategoryAsync(
            slug: $"parent-{TestToken}");

        var child = await Fixture.GivenCategoryAsync(
            slug: $"child-{TestToken}",
            parentCategoryId: parent.Id);

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: child.Id,
            rowVersion: child.RowVersion,
            newParentCategoryId: (long?)null);
        commandResult.Should().BeSuccess();

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Path.Should().Be(child.CurrentSlug.Value);
        payload.ParentCategoryId.Should().BeNull();
        payload.RowVersion.Should().BeGreaterThan(child.RowVersion);
        payload.UpdatedAt.Should().Be(actionTimestamp);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == child.Id, ct));

        categoryAfter.ParentCategoryId.Should().BeNull();
        categoryAfter.Path.Should().HaveValue(child.CurrentSlug.Value);
        categoryAfter.RowVersion.Should().Be(payload.RowVersion);
        categoryAfter.UpdatedAt.Should().Be(payload.UpdatedAt);
    }

    [Fact]
    public async Task ReparentCategory_ShouldReparent_WhenCategoryHasNoDescendants()
    {
        var originalParent = await Fixture.GivenCategoryAsync(
            slug: $"parent1-{TestToken}");

        var newParent = await Fixture.GivenCategoryAsync(
            slug: $"parent2-{TestToken}");

        var child = await Fixture.GivenCategoryAsync(
            slug: $"child-{TestToken}",
            parentCategoryId: originalParent.Id);

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: child.Id,
            rowVersion: child.RowVersion,
            newParentCategoryId: newParent.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Path.Should().Be($"{newParent.Path.Value}.{child.CurrentSlug.Value}");
        payload.ParentCategoryId.Should().Be(newParent.Id);
        payload.RowVersion.Should().BeGreaterThan(child.RowVersion);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == child.Id, ct));

        categoryAfter.ParentCategoryId.Should().Be(newParent.Id);
        categoryAfter.Path.Should().HaveValue($"{newParent.Path.Value}.{child.CurrentSlug.Value}");
        categoryAfter.RowVersion.Should().Be(payload.RowVersion);
    }

    [Fact]
    public async Task ReparentCategory_ShouldReturnExistingValues_WhenNewParentIdSameAsCurrent()
    {
        var parent = await Fixture.GivenCategoryAsync(
            slug: $"parent-{TestToken}");

        var child = await Fixture.GivenCategoryAsync(
            slug: $"child-{TestToken}",
            parentCategoryId: parent.Id);

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: child.Id,
            rowVersion: child.RowVersion,
            newParentCategoryId: parent.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Path.Should().Be(child.Path.Value);
        payload.ParentCategoryId.Should().Be(parent.Id);
        payload.RowVersion.Should().Be(child.RowVersion);
        payload.UpdatedAt.Should().Be(child.UpdatedAt);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == child.Id, ct));

        categoryAfter.RowVersion.Should().Be(child.RowVersion);
        categoryAfter.UpdatedAt.Should().Be(child.UpdatedAt);
        categoryAfter.Path.Should().HaveValue(child.Path.Value);
    }

    [Fact]
    public async Task ReparentCategory_ShouldFail_WhenNewParentNotFound()
    {
        var category = await Fixture.GivenCategoryAsync(
            slug: $"category-{TestToken}");
        const long nonExistentParentId = 999_999L;

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: category.Id,
            rowVersion: category.RowVersion,
            newParentCategoryId: nonExistentParentId);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.ParentNotFound>();
        error.ParentCategoryId.Should().Be(nonExistentParentId);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.ParentCategoryId.Should().Be(category.ParentCategoryId);
        categoryAfter.Path.Should().HaveValue(category.Path.Value);
        categoryAfter.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task ReparentCategory_ShouldFail_WhenNameNotUniqueWithinNewParent()
    {
        var parent1 = await Fixture.GivenCategoryAsync(
            name: $"Parent1-{TestToken}",
            slug: $"parent1-{TestToken}");

        var parent2 = await Fixture.GivenCategoryAsync(
            name: $"Parent2-{TestToken}",
            slug: $"parent2-{TestToken}");

        var sharedName = $"SharedName-{TestToken}";

        var childUnderParent1 = await Fixture.GivenCategoryAsync(
            name: sharedName,
            slug: $"shared1-{TestToken}",
            parentCategoryId: parent1.Id);

        var childUnderParent2 = await Fixture.GivenCategoryAsync(
            name: sharedName,
            slug: $"shared2-{TestToken}",
            parentCategoryId: parent2.Id);

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: childUnderParent2.Id,
            rowVersion: childUnderParent2.RowVersion,
            newParentCategoryId: parent1.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NameNotUniqueWithinParent>();
        error.Name.Should().Be(sharedName);
        error.ParentCategoryId.Should().Be(parent1.Id);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == childUnderParent2.Id, ct));

        categoryAfter.ParentCategoryId.Should().Be(parent2.Id);
        categoryAfter.Path.Should().HaveValue(childUnderParent2.Path.Value);
        categoryAfter.RowVersion.Should().Be(childUnderParent2.RowVersion);
    }

    [Fact]
    public async Task ReparentCategory_ShouldFail_WhenCircularDependencyWouldOccur()
    {
        var parent = await Fixture.GivenCategoryAsync(
            slug: $"parent-{TestToken}");

        var child = await Fixture.GivenCategoryAsync(
            slug: $"child-{TestToken}",
            parentCategoryId: parent.Id);

        var grandchild = await Fixture.GivenCategoryAsync(
            slug: $"grandchild-{TestToken}",
            parentCategoryId: child.Id);

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: parent.Id,
            rowVersion: parent.RowVersion,
            newParentCategoryId: grandchild.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.CircularDependency>();
        error.ParentId.Should().Be(grandchild.Id);
        error.CategoryId.Should().Be(parent.Id);

        var parentAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == parent.Id, ct));

        parentAfter.ParentCategoryId.Should().BeNull();
        parentAfter.Path.Should().HaveValue(parent.Path.Value);
        parentAfter.RowVersion.Should().Be(parent.RowVersion);
    }

    [Fact]
    public async Task ReparentCategory_ShouldFail_WhenDescendantLimitExceeded()
    {
        var category = await Fixture.GivenCategoryAsync(
            name: $"Category-{TestToken}",
            slug: $"category-{TestToken}");

        for (var i = 1; i <= Category.DescendantLimitOnUpdate + 1; i++)
        {
            await Fixture.GivenCategoryAsync(
                name: $"Child-{i}-{TestToken}",
                slug: $"child-{i}-{TestToken}",
                parentCategoryId: category.Id);
        }

        var newParent = await Fixture.GivenCategoryAsync(
            name: $"NewParent-{TestToken}",
            slug: $"new-parent-{TestToken}");

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: category.Id,
            rowVersion: category.RowVersion,
            newParentCategoryId: newParent.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.TooManyDescendants>();
        error.CategoryId.Should().Be(category.Id);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.ParentCategoryId.Should().BeNull();
        categoryAfter.Path.Should().HaveValue(category.Path.Value);
        categoryAfter.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task ReparentCategory_ShouldFail_WhenHierarchicalPathWouldBeTooDeep()
    {
        // Build a deep hierarchy that matches the max depth
        Category? previousCategory = null;
        for (var i = 1; i <= HierarchicalPath.MaxDepth; i++)
        {
            var category = await Fixture.GivenCategoryAsync(
                name: $"Level{i}-{TestToken}",
                slug: $"level{i}-{TestToken}",
                parentCategoryId: previousCategory?.Id);
            previousCategory = category;
        }

        var categoryToMove = await Fixture.GivenCategoryAsync(
            name: $"ToMove-{TestToken}",
            slug: $"to-move-{TestToken}");

        // Attempting to reparent under the deepest category would exceed max depth
        var commandResult = ReparentCategoryCommand.Create(
            categoryId: categoryToMove.Id,
            rowVersion: categoryToMove.RowVersion,
            newParentCategoryId: previousCategory!.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<HierarchicalPathErrors.TooDeep>();
        error.Depth.Should().Be(HierarchicalPath.MaxDepth + 1);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryToMove.Id, ct));

        categoryAfter.ParentCategoryId.Should().BeNull();
        categoryAfter.Path.Should().HaveValue(categoryToMove.Path.Value);
        categoryAfter.RowVersion.Should().Be(categoryToMove.RowVersion);
    }

    [Fact]
    public async Task ReparentCategory_ShouldFail_WhenRowVersionMismatch()
    {
        var category = await Fixture.GivenCategoryAsync(
            slug: $"category-{TestToken}");

        var newParent = await Fixture.GivenCategoryAsync(
            slug: $"new-parent-{TestToken}");

        var staleRowVersion = category.RowVersion + 1;

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: category.Id,
            rowVersion: staleRowVersion,
            newParentCategoryId: newParent.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == category.Id, ct));

        categoryAfter.ParentCategoryId.Should().BeNull();
        categoryAfter.Path.Should().HaveValue(category.Path.Value);
        categoryAfter.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task ReparentCategory_ShouldFail_WhenCategoryNotFound()
    {
        const long missingCategoryId = 999_999L;

        var commandResult = ReparentCategoryCommand.Create(
            categoryId: missingCategoryId,
            rowVersion: 1,
            newParentCategoryId: (long?)null);
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
