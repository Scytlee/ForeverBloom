using FluentAssertions;
using ForeverBloom.Application.Categories.Queries.GetCategoriesSitemapData;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.IntegrationTests.Categories.Queries;

public sealed class GetCategoriesSitemapDataUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task GetCategoriesSitemapData_ShouldReturnPublishedCategoriesWithValidAncestorChain_OrderedByMostRecentFirst()
    {
        var baseline = Fixture.TimeProvider.CurrentTime;

        var rootSlug = $"root-{TestToken}";
        var root = await Fixture.GivenCategoryAsync(
            slug: rootSlug,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(1));

        var childSlug = $"child-{TestToken}";
        await Fixture.GivenCategoryAsync(
            slug: childSlug,
            parentCategoryId: root.Id,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(2));

        var anotherRootSlug = $"another-root-{TestToken}";
        await Fixture.GivenCategoryAsync(
            slug: anotherRootSlug,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(3));

        var query = new GetCategoriesSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().HaveCount(3);
        payload.Items.Should().Contain(item => item.Slug == rootSlug);
        payload.Items.Should().Contain(item => item.Slug == childSlug);
        payload.Items.Should().Contain(item => item.Slug == anotherRootSlug);

        // Verify ordering - most recent first
        payload.Items.Should().BeInDescendingOrder(item => item.UpdatedAt);
        payload.Items[0].Slug.Should().Be(anotherRootSlug);
    }

    [Fact]
    public async Task GetCategoriesSitemapData_ShouldExcludeDraftAndArchivedCategories()
    {
        var publishedSlug = $"published-{TestToken}";
        await Fixture.GivenCategoryAsync(
            slug: publishedSlug,
            publishStatus: PublishStatus.Published);

        await Fixture.GivenCategoryAsync(
            slug: $"draft-{TestToken}",
            publishStatus: PublishStatus.Draft);

        await Fixture.GivenCategoryAsync(
            slug: $"archived-{TestToken}",
            publishStatus: PublishStatus.Published,
            isArchived: true);

        var query = new GetCategoriesSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().OnlyContain(item => item.Slug == publishedSlug);
    }

    [Fact]
    public async Task GetCategoriesSitemapData_ShouldExcludePublishedCategories_WhenAnyAncestorIsUnpublished()
    {
        var parent = await Fixture.GivenCategoryAsync(
            slug: $"unpublished-parent-{TestToken}",
            publishStatus: PublishStatus.Draft);

        var childSlug = $"published-child-{TestToken}";
        await Fixture.GivenCategoryAsync(
            slug: childSlug,
            parentCategoryId: parent.Id,
            publishStatus: PublishStatus.Published);

        var query = new GetCategoriesSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategoriesSitemapData_ShouldExcludePublishedCategories_WhenAnyAncestorIsArchived()
    {
        var parent = await Fixture.GivenCategoryAsync(
            slug: $"archived-parent-{TestToken}",
            publishStatus: PublishStatus.Published);

        var childSlug = $"published-child-{TestToken}";
        await Fixture.GivenCategoryAsync(
            slug: childSlug,
            parentCategoryId: parent.Id,
            publishStatus: PublishStatus.Published);

        await Fixture.ArchiveExistingCategoryAsync(parent);

        var query = new GetCategoriesSitemapDataQuery();
        var result = await SendAsync(query, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().BeEmpty();
    }
}
