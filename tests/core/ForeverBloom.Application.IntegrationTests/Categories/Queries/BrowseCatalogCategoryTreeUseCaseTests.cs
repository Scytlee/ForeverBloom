using ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;

namespace ForeverBloom.Application.IntegrationTests.Categories.Queries;

public sealed class BrowseCatalogCategoryTreeUseCaseTests : ApplicationIntegrationTestBase
{
    /// <summary>
    /// Creates a standard test hierarchy:
    ///   Root1 -> Child1A -> Grandchild1A1
    ///                    -> Grandchild1A2
    ///         -> Child1B
    ///   Root2 -> Child2A
    /// All categories are published by default unless specified otherwise.
    /// </summary>
    private async Task<CategoryHierarchy> ArrangeStandardHierarchyAsync(
        PublishStatus? root1Status = null,
        PublishStatus? child1AStatus = null,
        PublishStatus? grandchild1A1Status = null,
        PublishStatus? grandchild1A2Status = null,
        PublishStatus? child1BStatus = null,
        PublishStatus? root2Status = null,
        PublishStatus? child2AStatus = null,
        bool archiveChild1A = false)
    {
        var root1 = await Fixture.GivenCategoryAsync(
            name: $"Root1-{TestToken}",
            slug: $"root1-{TestToken}",
            publishStatus: root1Status ?? PublishStatus.Published);

        var child1A = await Fixture.GivenCategoryAsync(
            name: $"Child1A-{TestToken}",
            slug: $"child1a-{TestToken}",
            parentCategoryId: root1.Id,
            publishStatus: child1AStatus ?? PublishStatus.Published);

        var grandchild1A1 = await Fixture.GivenCategoryAsync(
            name: $"Grandchild1A1-{TestToken}",
            slug: $"grandchild1a1-{TestToken}",
            parentCategoryId: child1A.Id,
            publishStatus: grandchild1A1Status ?? PublishStatus.Published);

        var grandchild1A2 = await Fixture.GivenCategoryAsync(
            name: $"Grandchild1A2-{TestToken}",
            slug: $"grandchild1a2-{TestToken}",
            parentCategoryId: child1A.Id,
            publishStatus: grandchild1A2Status ?? PublishStatus.Published);

        var child1B = await Fixture.GivenCategoryAsync(
            name: $"Child1B-{TestToken}",
            slug: $"child1b-{TestToken}",
            parentCategoryId: root1.Id,
            publishStatus: child1BStatus ?? PublishStatus.Published);

        var root2 = await Fixture.GivenCategoryAsync(
            name: $"Root2-{TestToken}",
            slug: $"root2-{TestToken}",
            publishStatus: root2Status ?? PublishStatus.Published);

        var child2A = await Fixture.GivenCategoryAsync(
            name: $"Child2A-{TestToken}",
            slug: $"child2a-{TestToken}",
            parentCategoryId: root2.Id,
            publishStatus: child2AStatus ?? PublishStatus.Published);

        if (archiveChild1A)
        {
            await Fixture.ArchiveExistingCategoryAsync(child1A);
        }

        return new CategoryHierarchy
        {
            Root1 = root1,
            Child1A = child1A,
            Grandchild1A1 = grandchild1A1,
            Grandchild1A2 = grandchild1A2,
            Child1B = child1B,
            Root2 = root2,
            Child2A = child2A
        };
    }

    [Fact]
    public async Task BrowseCatalogCategoryTree_ShouldReturnEntireTree_WhenNoParametersProvided()
    {
        var hierarchy = await ArrangeStandardHierarchyAsync();

        // Root: none, Levels: none
        // Expected hierarchy:
        //   Root1 -> Child1A -> Grandchild1A1
        //                    -> Grandchild1A2
        //         -> Child1B
        //   Root2 -> Child2A
        var queryResult = BrowseCatalogCategoryTreeQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Categories.Should().HaveCount(2);

        var root1Result = payload.Categories.Should().Contain(c => c.Id == hierarchy.Root1.Id).Subject;
        root1Result.Name.Should().Be(hierarchy.Root1.Name.Value);
        root1Result.Slug.Should().Be(hierarchy.Root1.CurrentSlug.Value);
        root1Result.Children.Should().HaveCount(2);

        var child1AResult = root1Result.Children.Should().Contain(c => c.Id == hierarchy.Child1A.Id).Subject;
        child1AResult.Name.Should().Be(hierarchy.Child1A.Name.Value);
        child1AResult.Slug.Should().Be(hierarchy.Child1A.CurrentSlug.Value);
        child1AResult.Children.Should().HaveCount(2);

        var grandchild1A1Result = child1AResult.Children.Should().Contain(c => c.Id == hierarchy.Grandchild1A1.Id).Subject;
        grandchild1A1Result.Name.Should().Be(hierarchy.Grandchild1A1.Name.Value);
        grandchild1A1Result.Slug.Should().Be(hierarchy.Grandchild1A1.CurrentSlug.Value);
        grandchild1A1Result.Children.Should().BeEmpty();

        var grandchild1A2Result = child1AResult.Children.Should().Contain(c => c.Id == hierarchy.Grandchild1A2.Id).Subject;
        grandchild1A2Result.Name.Should().Be(hierarchy.Grandchild1A2.Name.Value);
        grandchild1A2Result.Slug.Should().Be(hierarchy.Grandchild1A2.CurrentSlug.Value);
        grandchild1A2Result.Children.Should().BeEmpty();

        var child1BResult = root1Result.Children.Should().Contain(c => c.Id == hierarchy.Child1B.Id).Subject;
        child1BResult.Name.Should().Be(hierarchy.Child1B.Name.Value);
        child1BResult.Slug.Should().Be(hierarchy.Child1B.CurrentSlug.Value);
        child1BResult.Children.Should().BeEmpty();

        var root2Result = payload.Categories.Should().Contain(c => c.Id == hierarchy.Root2.Id).Subject;
        root2Result.Name.Should().Be(hierarchy.Root2.Name.Value);
        root2Result.Slug.Should().Be(hierarchy.Root2.CurrentSlug.Value);
        root2Result.Children.Should().HaveCount(1);

        var child2AResult = root2Result.Children.Should().Contain(c => c.Id == hierarchy.Child2A.Id).Subject;
        child2AResult.Name.Should().Be(hierarchy.Child2A.Name.Value);
        child2AResult.Slug.Should().Be(hierarchy.Child2A.CurrentSlug.Value);
        child2AResult.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task BrowseCatalogCategoryTree_ShouldReturnEmptyList_WhenNoPublishedCategoriesExist()
    {
        await ArrangeStandardHierarchyAsync(
            root1Status: PublishStatus.Draft,
            child1AStatus: PublishStatus.Draft,
            grandchild1A1Status: PublishStatus.Draft,
            grandchild1A2Status: PublishStatus.Draft,
            child1BStatus: PublishStatus.Draft,
            root2Status: PublishStatus.Draft,
            child2AStatus: PublishStatus.Draft);

        var queryResult = BrowseCatalogCategoryTreeQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task BrowseCatalogCategoryTree_ShouldReturnSubtree_WhenRootCategoryIdProvided()
    {
        var hierarchy = await ArrangeStandardHierarchyAsync();

        // Root: Child1A, Levels: none
        // Expected hierarchy:
        //   Child1A -> Grandchild1A1
        //           -> Grandchild1A2
        var queryResult = BrowseCatalogCategoryTreeQuery.Create(rootCategoryId: hierarchy.Child1A.Id);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Categories.Should().HaveCount(1);

        var child1AResult = payload.Categories.Should().Contain(c => c.Id == hierarchy.Child1A.Id).Subject;
        child1AResult.Children.Should().HaveCount(2);

        var grandchild1A1Result = child1AResult.Children.Should().Contain(c => c.Id == hierarchy.Grandchild1A1.Id).Subject;
        grandchild1A1Result.Children.Should().BeEmpty();

        var grandchild1A2Result = child1AResult.Children.Should().Contain(c => c.Id == hierarchy.Grandchild1A2.Id).Subject;
        grandchild1A2Result.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task BrowseCatalogCategoryTree_ShouldLimitLevels_WhenLevelsParameterProvided()
    {
        var hierarchy = await ArrangeStandardHierarchyAsync();

        // Root: none, Levels: 2
        // Expected hierarchy:
        //   Root1 -> Child1A
        //         -> Child1B
        //   Root2 -> Child2A
        var queryResult = BrowseCatalogCategoryTreeQuery.Create(levels: 2);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Categories.Should().HaveCount(2);

        var root1Result = payload.Categories.Should().Contain(c => c.Id == hierarchy.Root1.Id).Subject;
        root1Result.Children.Should().HaveCount(2);

        var child1AResult = root1Result.Children.Should().Contain(c => c.Id == hierarchy.Child1A.Id).Subject;
        child1AResult.Children.Should().BeEmpty();

        var child1BResult = root1Result.Children.Should().Contain(c => c.Id == hierarchy.Child1B.Id).Subject;
        child1BResult.Children.Should().BeEmpty();

        var root2Result = payload.Categories.Should().Contain(c => c.Id == hierarchy.Root2.Id).Subject;
        root2Result.Children.Should().HaveCount(1);

        var child2AResult = root2Result.Children.Should().Contain(c => c.Id == hierarchy.Child2A.Id).Subject;
        child2AResult.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task BrowseCatalogCategoryTree_ShouldCombineRootAndLevels_WhenBothProvided()
    {
        var hierarchy = await ArrangeStandardHierarchyAsync();

        // Root: Root1, Levels: 2
        // Expected hierarchy:
        //   Root1 -> Child1A
        //         -> Child1B
        var queryResult = BrowseCatalogCategoryTreeQuery.Create(
            rootCategoryId: hierarchy.Root1.Id,
            levels: 2);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Categories.Should().HaveCount(1);

        var root1Result = payload.Categories.Should().Contain(c => c.Id == hierarchy.Root1.Id).Subject;
        root1Result.Children.Should().HaveCount(2);

        var child1AResult = root1Result.Children.Should().Contain(c => c.Id == hierarchy.Child1A.Id).Subject;
        child1AResult.Children.Should().BeEmpty();

        var child1BResult = root1Result.Children.Should().Contain(c => c.Id == hierarchy.Child1B.Id).Subject;
        child1BResult.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task BrowseCatalogCategoryTree_ShouldExcludeUnpublishedAncestors_AndTheirDescendants()
    {
        var hierarchy = await ArrangeStandardHierarchyAsync(
            child1AStatus: PublishStatus.Draft);

        // Child1A is unpublished
        // Root: none, Levels: none
        // Expected hierarchy:
        //   Root1 -> Child1B
        //   Root2 -> Child2A
        var queryResult = BrowseCatalogCategoryTreeQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Categories.Should().HaveCount(2);

        var root1Result = payload.Categories.Should().Contain(c => c.Id == hierarchy.Root1.Id).Subject;
        root1Result.Children.Should().HaveCount(1);

        var child1BResult = root1Result.Children.Should().Contain(c => c.Id == hierarchy.Child1B.Id).Subject;
        child1BResult.Children.Should().BeEmpty();

        var root2Result = payload.Categories.Should().Contain(c => c.Id == hierarchy.Root2.Id).Subject;
        root2Result.Children.Should().HaveCount(1);

        var child2AResult = root2Result.Children.Should().Contain(c => c.Id == hierarchy.Child2A.Id).Subject;
        child2AResult.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task BrowseCatalogCategoryTree_ShouldExcludeArchivedCategories_AndTheirDescendants()
    {
        var hierarchy = await ArrangeStandardHierarchyAsync(archiveChild1A: true);

        // Child1A is archived
        // Root: none, Levels: none
        // Expected hierarchy:
        //   Root1 -> Child1B
        //   Root2 -> Child2A
        var queryResult = BrowseCatalogCategoryTreeQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Categories.Should().HaveCount(2);

        var root1Result = payload.Categories.Should().Contain(c => c.Id == hierarchy.Root1.Id).Subject;
        root1Result.Children.Should().HaveCount(1);

        var child1BResult = root1Result.Children.Should().Contain(c => c.Id == hierarchy.Child1B.Id).Subject;
        child1BResult.Children.Should().BeEmpty();

        var root2Result = payload.Categories.Should().Contain(c => c.Id == hierarchy.Root2.Id).Subject;
        root2Result.Children.Should().HaveCount(1);

        var child2AResult = root2Result.Children.Should().Contain(c => c.Id == hierarchy.Child2A.Id).Subject;
        child2AResult.Children.Should().BeEmpty();
    }

    private sealed class CategoryHierarchy
    {
        public Category Root1 { get; init; } = null!;
        public Category Child1A { get; init; } = null!;
        public Category Grandchild1A1 { get; init; } = null!;
        public Category Grandchild1A2 { get; init; } = null!;
        public Category Child1B { get; init; } = null!;
        public Category Root2 { get; init; } = null!;
        public Category Child2A { get; init; } = null!;
    }
}
