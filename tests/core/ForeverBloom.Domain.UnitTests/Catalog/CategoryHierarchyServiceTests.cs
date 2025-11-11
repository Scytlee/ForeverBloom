using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class CategoryHierarchyServiceTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private readonly CategoryHierarchyService _service = new();

    [Fact]
    public void ReparentCategoryAndRebaseDescendants_ShouldSucceed_AndUpdateDescendants()
    {
        // Create a category hierarchy
        // root
        //   category (moving this to new-parent)
        //     child
        //       grandchild
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "category",
            path: "root.category",
            parentCategoryId: 1);
        var child = CategoryFactory.Create(
            TestTimestamp,
            slug: "child",
            path: "root.category.child");
        var grandchild = CategoryFactory.Create(
            TestTimestamp,
            slug: "grandchild",
            path: "root.category.child.grandchild");

        var descendants = new[] { child, grandchild };
        var newParentPath = HierarchicalPathFactory.Create("new-parent");

        var result = _service.ReparentCategoryAndRebaseDescendants(
            category,
            newParentId: 2,
            newParentPath,
            descendants,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.ParentCategoryId.Should().Be(2);
        category.Path.Value.Should().Be("new-parent.category");
        child.Path.Value.Should().Be("new-parent.category.child");
        grandchild.Path.Value.Should().Be("new-parent.category.child.grandchild");
    }

    [Fact]
    public void ReparentCategoryAndRebaseDescendants_ShouldSucceed_MovingToRoot()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "category",
            path: "parent.category",
            parentCategoryId: 1);
        var child = CategoryFactory.Create(
            TestTimestamp,
            slug: "child",
            path: "parent.category.child");

        var descendants = new[] { child };

        var result = _service.ReparentCategoryAndRebaseDescendants(
            category,
            newParentId: null,
            newParentPath: null,
            descendants,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.ParentCategoryId.Should().BeNull();
        category.Path.Value.Should().Be("category");
        child.Path.Value.Should().Be("category.child");
    }

    [Fact]
    public void ReparentCategoryAndRebaseDescendants_ShouldReturnFalse_WhenParentUnchanged()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "category",
            path: "parent.category",
            parentCategoryId: 1);
        var child = CategoryFactory.Create(
            TestTimestamp,
            slug: "child",
            path: "parent.category.child");

        var descendants = new[] { child };
        var sameParentPath = HierarchicalPathFactory.Create("parent");

        var result = _service.ReparentCategoryAndRebaseDescendants(
            category,
            newParentId: 1,
            sameParentPath,
            descendants,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void ReparentCategoryAndRebaseDescendants_ShouldFail_WhenCircularDependency()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "category",
            path: "category",
            id: 5);
        var child = CategoryFactory.Create(
            TestTimestamp,
            slug: "child",
            path: "category.child",
            id: 10);

        var descendants = new[] { child };
        var childPath = HierarchicalPathFactory.Create("category.child");

        var result = _service.ReparentCategoryAndRebaseDescendants(
            category,
            newParentId: 10,
            childPath,
            descendants,
            TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<CategoryErrors.CircularDependency>();
        error.CategoryId.Should().Be(5);
        error.AttemptedParentId.Should().Be(10);
    }

    [Fact]
    public void ReparentCategoryAndRebaseDescendants_ShouldFail_WhenInvalidParentId()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var descendants = Array.Empty<Category>();
        var newParentPath = HierarchicalPathFactory.Create("parent");

        var result = _service.ReparentCategoryAndRebaseDescendants(
            category,
            newParentId: 0,
            newParentPath,
            descendants,
            TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        result.Should().HaveSingleError<CategoryErrors.ParentCategoryIdInvalid>();
    }

    [Fact]
    public void ReparentCategoryAndRebaseDescendants_ShouldThrow_WhenParentIdAndPathInconsistent()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var descendants = Array.Empty<Category>();
        var newParentPath = HierarchicalPathFactory.Create("parent");

        var action = () => _service.ReparentCategoryAndRebaseDescendants(
            category,
            newParentId: null,
            newParentPath,
            descendants,
            TestTimestamp.AddHours(1));

        action.Should().Throw<ArgumentException>()
            .WithMessage("newParentId and newParentPath must both be provided or null.");
    }

    [Fact]
    public void ReparentCategoryAndRebaseDescendants_ShouldFail_WhenDepthExceeded()
    {
        // Create a hierarchy that will exceed max depth (10) when reparented
        // Current: a.b.c.category (depth 4)
        //          a.b.c.category.child.grandchild.great (depth 7)
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "category",
            path: "a.b.c.category");

        var descendant = CategoryFactory.Create(
            TestTimestamp,
            slug: "great",
            path: "a.b.c.category.child.grandchild.great");

        var descendants = new[] { descendant };

        // Move to a parent with depth 7: x.y.z.w.v.u.t
        // New paths would be:
        // - category: x.y.z.w.v.u.t.category (depth 8)
        // - descendant: x.y.z.w.v.u.t.category.child.grandchild.great (depth 11) - exceeds max
        var deepParentPath = HierarchicalPathFactory.Create("x.y.z.w.v.u.t");

        var result = _service.ReparentCategoryAndRebaseDescendants(
            category,
            newParentId: 99,
            deepParentPath,
            descendants,
            TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        result.Should().HaveSingleError<HierarchicalPathErrors.TooDeep>();
    }

    [Fact]
    public void ChangeCategorySlugAndRebaseDescendants_ShouldSucceed_AndUpdateDescendants()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "old-slug",
            path: "parent.old-slug");
        var child = CategoryFactory.Create(
            TestTimestamp,
            slug: "child",
            path: "parent.old-slug.child");
        var grandchild = CategoryFactory.Create(
            TestTimestamp,
            slug: "grandchild",
            path: "parent.old-slug.child.grandchild");

        var descendants = new[] { child, grandchild };
        var newSlug = SlugFactory.Create("new-slug");

        var result = _service.ChangeCategorySlugAndRebaseDescendants(
            category,
            newSlug,
            descendants,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.CurrentSlug.Should().Be(newSlug);
        category.Path.Value.Should().Be("parent.new-slug");
        child.Path.Value.Should().Be("parent.new-slug.child");
        grandchild.Path.Value.Should().Be("parent.new-slug.child.grandchild");
    }

    [Fact]
    public void ChangeCategorySlugAndRebaseDescendants_ShouldReturnFalse_WhenSlugUnchanged()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "same-slug",
            path: "parent.same-slug");
        var child = CategoryFactory.Create(
            TestTimestamp,
            slug: "child",
            path: "parent.same-slug.child");

        var descendants = new[] { child };
        var sameSlug = SlugFactory.Create("same-slug");

        var result = _service.ChangeCategorySlugAndRebaseDescendants(
            category,
            sameSlug,
            descendants,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void ArchiveCategoryAndDescendants_ShouldSucceed_AndArchiveAll()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var child = CategoryFactory.Create(TestTimestamp);
        var grandchild = CategoryFactory.Create(TestTimestamp);

        var descendants = new[] { child, grandchild };
        var archiveTime = TestTimestamp.AddHours(1);

        var result = _service.ArchiveCategoryAndDescendants(
            category,
            descendants,
            archiveTime);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.DeletedAt.Should().Be(archiveTime);
        category.IsDeleted.Should().BeTrue();
        child.DeletedAt.Should().Be(archiveTime);
        child.IsDeleted.Should().BeTrue();
        grandchild.DeletedAt.Should().Be(archiveTime);
        grandchild.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void ArchiveCategoryAndDescendants_ShouldReturnFalse_WhenAlreadyArchived()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var child = CategoryFactory.Create(TestTimestamp);

        var descendants = new[] { child };

        // Archive first time
        category.Archive(TestTimestamp.AddHours(1));

        // Try to archive again
        var result = _service.ArchiveCategoryAndDescendants(
            category,
            descendants,
            TestTimestamp.AddHours(2));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        category.DeletedAt.Should().Be(TestTimestamp.AddHours(1)); // Unchanged
    }

    [Fact]
    public void ArchiveCategoryAndDescendants_ShouldSucceed_WithEmptyDescendants()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var descendants = Array.Empty<Category>();
        var archiveTime = TestTimestamp.AddHours(1);

        var result = _service.ArchiveCategoryAndDescendants(
            category,
            descendants,
            archiveTime);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.DeletedAt.Should().Be(archiveTime);
    }

    [Fact]
    public void RestoreCategoryAndDescendants_ShouldSucceed_AndRestoreAll()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var child = CategoryFactory.Create(TestTimestamp);
        var grandchild = CategoryFactory.Create(TestTimestamp);

        var descendants = new[] { child, grandchild };

        // Archive first
        category.Archive(TestTimestamp.AddHours(1));
        child.Archive(TestTimestamp.AddHours(1));
        grandchild.Archive(TestTimestamp.AddHours(1));

        // Restore
        var result = _service.RestoreCategoryAndDescendants(category, descendants);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.DeletedAt.Should().BeNull();
        category.IsDeleted.Should().BeFalse();
        child.DeletedAt.Should().BeNull();
        child.IsDeleted.Should().BeFalse();
        grandchild.DeletedAt.Should().BeNull();
        grandchild.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void RestoreCategoryAndDescendants_ShouldReturnFalse_WhenNotArchived()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var child = CategoryFactory.Create(TestTimestamp);

        var descendants = new[] { child };

        var result = _service.RestoreCategoryAndDescendants(category, descendants);

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        category.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void RestoreCategoryAndDescendants_ShouldSucceed_WithEmptyDescendants()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var descendants = Array.Empty<Category>();

        // Archive first
        category.Archive(TestTimestamp.AddHours(1));

        // Restore
        var result = _service.RestoreCategoryAndDescendants(category, descendants);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.DeletedAt.Should().BeNull();
    }
}
