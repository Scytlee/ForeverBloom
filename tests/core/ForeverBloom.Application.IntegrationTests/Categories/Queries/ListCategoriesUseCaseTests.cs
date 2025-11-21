using ForeverBloom.Application.Categories.Queries.ListCategories;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Result;
using FluentAssertions;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Testing.Integration.Seeding;

namespace ForeverBloom.Application.IntegrationTests.Categories.Queries;

public sealed class ListCategoriesUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task ListCategories_ShouldReturnPagedResults_WithDefaultPagination()
    {
        var category1 = await Fixture.GivenCategoryAsync();
        var category2 = await Fixture.GivenCategoryAsync();
        var category3 = await Fixture.GivenCategoryAsync();

        var queryResult = ListCategoriesQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(3);
        payload.PageNumber.Should().Be(PaginationConstants.DefaultPageNumber);
        payload.PageSize.Should().Be(PaginationConstants.DefaultPageSize);

        payload.Items.Should().Contain(c => c.Id == category1.Id);
        payload.Items.Should().Contain(c => c.Id == category2.Id);
        payload.Items.Should().Contain(c => c.Id == category3.Id);
    }

    [Fact]
    public async Task ListCategories_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        var queryResult = ListCategoriesQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(0);
        payload.PageNumber.Should().Be(PaginationConstants.DefaultPageNumber);
        payload.PageSize.Should().Be(PaginationConstants.DefaultPageSize);
    }

    [Fact]
    public async Task ListCategories_ShouldApplySorting()
    {
        var categoryC = await Fixture.GivenCategoryAsync(
            name: $"Charlie-{TestToken}");

        var categoryA = await Fixture.GivenCategoryAsync(
            name: $"Alpha-{TestToken}");

        var categoryB = await Fixture.GivenCategoryAsync(
            name: $"Bravo-{TestToken}");

        var queryResult = ListCategoriesQuery.Create(sortBy: "name:asc");
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(3);
        payload.Items[0].Id.Should().Be(categoryA.Id);
        payload.Items[1].Id.Should().Be(categoryB.Id);
        payload.Items[2].Id.Should().Be(categoryC.Id);
    }

    [Fact]
    public async Task ListCategories_ShouldFilterBySearchTerm()
    {
        var matchingCategory1 = await Fixture.GivenCategoryAsync(
            name: $"Roses-{TestToken}",
            slug: $"roses-{TestToken}",
            description: "Beautiful flowers");

        var matchingCategory2 = await Fixture.GivenCategoryAsync(
            name: $"Tulips-{TestToken}",
            slug: $"tulips-{TestToken}",
            description: $"Spring roses collection {TestToken}");

        var nonMatchingCategory = await Fixture.GivenCategoryAsync(
            name: $"Orchids-{TestToken}",
            slug: $"orchids-{TestToken}",
            description: "Exotic plants");

        var queryResult = ListCategoriesQuery.Create(searchTerm: "roses");
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(2);
        payload.Items.Should().Contain(c => c.Id == matchingCategory1.Id);
        payload.Items.Should().Contain(c => c.Id == matchingCategory2.Id);
        payload.Items.Should().NotContain(c => c.Id == nonMatchingCategory.Id);
    }

    [Fact]
    public async Task ListCategories_ShouldFilterByPublishStatus()
    {
        var publishedCategory = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var draftCategory = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Draft);

        var queryResult = ListCategoriesQuery.Create(publishStatus: "Published");
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(1);
        payload.Items.Should().Contain(c => c.Id == publishedCategory.Id);
        payload.Items.Should().NotContain(c => c.Id == draftCategory.Id);
    }

    [Fact]
    public async Task ListCategories_ShouldFilterByRootCategory_DirectChildrenOnly()
    {
        // Arranged hierarchy:
        //   parent -> child1 -> grandchild
        //          -> child2
        //   otherRoot
        var parent = await Fixture.GivenCategoryAsync();

        var child1 = await Fixture.GivenCategoryAsync(
            parentCategoryId: parent.Id);

        var child2 = await Fixture.GivenCategoryAsync(
            parentCategoryId: parent.Id);

        var grandchild = await Fixture.GivenCategoryAsync(
            parentCategoryId: child1.Id);

        var otherRoot = await Fixture.GivenCategoryAsync();

        var queryResult = ListCategoriesQuery.Create(
            rootCategoryId: parent.Id,
            includeSubcategories: false);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(2);
        payload.Items.Should().Contain(c => c.Id == child1.Id);
        payload.Items.Should().Contain(c => c.Id == child2.Id);
        payload.Items.Should().NotContain(c => c.Id == parent.Id);
        payload.Items.Should().NotContain(c => c.Id == grandchild.Id);
        payload.Items.Should().NotContain(c => c.Id == otherRoot.Id);
    }

    [Fact]
    public async Task ListCategories_ShouldFilterByRootCategory_WithSubcategories()
    {
        // Arranged hierarchy:
        //   parent -> child1 -> grandchild
        //          -> child2
        //   otherRoot
        var parent = await Fixture.GivenCategoryAsync();

        var child1 = await Fixture.GivenCategoryAsync(
            parentCategoryId: parent.Id);

        var child2 = await Fixture.GivenCategoryAsync(
            parentCategoryId: parent.Id);

        var grandchild = await Fixture.GivenCategoryAsync(
            parentCategoryId: child1.Id);

        var otherRoot = await Fixture.GivenCategoryAsync();

        var queryResult = ListCategoriesQuery.Create(
            rootCategoryId: parent.Id,
            includeSubcategories: true);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(4);
        payload.Items.Should().Contain(c => c.Id == parent.Id);
        payload.Items.Should().Contain(c => c.Id == child1.Id);
        payload.Items.Should().Contain(c => c.Id == child2.Id);
        payload.Items.Should().Contain(c => c.Id == grandchild.Id);
        payload.Items.Should().NotContain(c => c.Id == otherRoot.Id);
    }

    [Fact]
    public async Task ListCategories_ShouldExcludeSoftDeletedCategories()
    {
        var activeCategory = await Fixture.GivenCategoryAsync();

        var deletedCategory = await Fixture.GivenCategoryAsync(
            isArchived: true);

        var queryResult = ListCategoriesQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(1);
        payload.Items.Should().Contain(c => c.Id == activeCategory.Id);
        payload.Items.Should().NotContain(c => c.Id == deletedCategory.Id);
    }
}
