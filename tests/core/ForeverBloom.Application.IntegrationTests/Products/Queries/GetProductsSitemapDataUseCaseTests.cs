using FluentAssertions;
using ForeverBloom.Application.Products.Queries.GetProductsSitemapData;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.IntegrationTests.Products.Queries;

public sealed class GetProductsSitemapDataUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task GetProductsSitemapData_ShouldReturnPublishedProductsWithValidCategoryChain_OrderedByMostRecentFirst()
    {
        var baseline = Fixture.TimeProvider.InitialTime;

        var category = await Fixture.GivenCategoryAsync(
            slug: $"flowers-{TestToken}",
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(1));

        var product1Slug = $"rose-{TestToken}";
        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: product1Slug,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(2));

        var product2Slug = $"tulip-{TestToken}";
        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: product2Slug,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(3));

        var product3Slug = $"lily-{TestToken}";
        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: product3Slug,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(4));

        var query = new GetProductsSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().HaveCount(3);
        payload.Items.Should().Contain(item => item.Slug == product1Slug);
        payload.Items.Should().Contain(item => item.Slug == product2Slug);
        payload.Items.Should().Contain(item => item.Slug == product3Slug);

        // Verify ordering - most recent first
        payload.Items.Should().BeInDescendingOrder(item => item.UpdatedAt);
        payload.Items[0].Slug.Should().Be(product3Slug);
    }

    [Fact]
    public async Task GetProductsSitemapData_ShouldExcludeDraftAndArchivedProducts()
    {
        var category = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var publishedSlug = $"published-{TestToken}";
        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: publishedSlug,
            publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: $"draft-{TestToken}",
            publishStatus: PublishStatus.Draft);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: $"archived-{TestToken}",
            publishStatus: PublishStatus.Published,
            isArchived: true);

        var query = new GetProductsSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().OnlyContain(item => item.Slug == publishedSlug);
    }

    [Fact]
    public async Task GetProductsSitemapData_ShouldExcludePublishedProducts_WhenCategoryIsDraft()
    {
        var category = await Fixture.GivenCategoryAsync(
            slug: $"draft-category-{TestToken}",
            publishStatus: PublishStatus.Draft);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: $"product-in-draft-category-{TestToken}",
            publishStatus: PublishStatus.Published);

        var query = new GetProductsSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductsSitemapData_ShouldExcludePublishedProducts_WhenCategoryIsArchived()
    {
        var category = await Fixture.GivenCategoryAsync(
            slug: $"archived-category-{TestToken}",
            publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: $"product-in-archived-category-{TestToken}",
            publishStatus: PublishStatus.Published);

        await Fixture.ArchiveExistingCategoryAsync(category);

        var query = new GetProductsSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductsSitemapData_ShouldExcludePublishedProducts_WhenCategoryHasUnpublishedAncestor()
    {
        var rootCategory = await Fixture.GivenCategoryAsync(
            slug: $"unpublished-root-{TestToken}",
            publishStatus: PublishStatus.Draft);

        var childCategory = await Fixture.GivenCategoryAsync(
            slug: $"published-child-{TestToken}",
            parentCategoryId: rootCategory.Id,
            publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
            categoryId: childCategory.Id,
            slug: $"product-{TestToken}",
            publishStatus: PublishStatus.Published);

        var query = new GetProductsSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductsSitemapData_ShouldExcludePublishedProducts_WhenCategoryHasArchivedAncestor()
    {
        var rootCategory = await Fixture.GivenCategoryAsync(
            slug: $"root-{TestToken}",
            publishStatus: PublishStatus.Published);

        var childCategory = await Fixture.GivenCategoryAsync(
            slug: $"child-{TestToken}",
            parentCategoryId: rootCategory.Id,
            publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
            categoryId: childCategory.Id,
            slug: $"product-{TestToken}",
            publishStatus: PublishStatus.Published);

        await Fixture.ArchiveExistingCategoryAsync(rootCategory);

        var query = new GetProductsSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductsSitemapData_ShouldReturnEmptyList_WhenNoPublishedProductsExist()
    {
        var category = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: $"draft-{TestToken}",
            publishStatus: PublishStatus.Draft);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: $"archived-{TestToken}",
            publishStatus: PublishStatus.Published,
            isArchived: true);

        var query = new GetProductsSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().BeEmpty();
    }
}
