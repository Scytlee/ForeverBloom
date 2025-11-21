using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;
using ForeverBloom.Application.Pagination;

namespace ForeverBloom.Application.IntegrationTests.Products.Queries;

public sealed class BrowseCatalogProductsUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task BrowseCatalogProducts_ShouldReturnEmptyList_WhenNoPublishedProductsExist()
    {
        var category = await Fixture.GivenCategoryAsync(publishStatus: PublishStatus.Published);
        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            publishStatus: PublishStatus.Draft);

        var queryResult = BrowseCatalogProductsQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(0);
        payload.PageNumber.Should().Be(PaginationConstants.DefaultPageNumber);
        payload.PageSize.Should().Be(PaginationConstants.DefaultPageSize);
    }

    [Fact]
    public async Task BrowseCatalogProducts_ShouldReturnPaginatedProducts_WithCustomPaginationSettings()
    {
        var category = await Fixture.GivenCategoryAsync(publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product1-{TestToken}",
            publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product2-{TestToken}",
            publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product3-{TestToken}",
            publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product4-{TestToken}",
            publishStatus: PublishStatus.Published);

        await Fixture.GivenProductAsync(
           categoryId: category.Id,
           name: $"Product5-{TestToken}",
           publishStatus: PublishStatus.Published);

        const int pageNumber = 2;
        const int pageSize = 2;

        var queryResult = BrowseCatalogProductsQuery.Create(
            pageNumber: pageNumber,
            pageSize: pageSize);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(5);
        payload.PageNumber.Should().Be(pageNumber);
        payload.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public async Task BrowseCatalogProducts_ShouldSortByRelevance_FeaturedFirstThenCreatedAtDesc()
    {
        var category = await Fixture.GivenCategoryAsync(publishStatus: PublishStatus.Published);

        var baseline = Fixture.TimeProvider.CurrentTime;

        var notFeaturedOld = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isFeatured: false,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(1));

        var featuredOld = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isFeatured: true,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(2));

        var featuredNew = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isFeatured: true,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(3));

        var notFeaturedNew = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isFeatured: false,
            publishStatus: PublishStatus.Published,
            creationTimestamp: baseline.AddDays(4));

        var queryResult = BrowseCatalogProductsQuery.Create(sortStrategy: "relevance");
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().HaveCount(4);
        payload.Items[0].Id.Should().Be(featuredNew.Id);
        payload.Items[1].Id.Should().Be(featuredOld.Id);
        payload.Items[2].Id.Should().Be(notFeaturedNew.Id);
        payload.Items[3].Id.Should().Be(notFeaturedOld.Id);
    }

    [Fact]
    public async Task BrowseCatalogProducts_ShouldFilterByCategoryHierarchically_IncludingDescendants()
    {
        var rootCategory = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var childCategory = await Fixture.GivenCategoryAsync(
            parentCategoryId: rootCategory.Id,
            publishStatus: PublishStatus.Published);

        var grandchildCategory = await Fixture.GivenCategoryAsync(
            parentCategoryId: childCategory.Id,
            publishStatus: PublishStatus.Published);

        var otherCategory = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var productInRoot = await Fixture.GivenProductAsync(
            categoryId: rootCategory.Id,
            publishStatus: PublishStatus.Published);

        var productInChild = await Fixture.GivenProductAsync(
            categoryId: childCategory.Id,
            publishStatus: PublishStatus.Published);

        var productInGrandchild = await Fixture.GivenProductAsync(
            categoryId: grandchildCategory.Id,
            publishStatus: PublishStatus.Published);

        var productInOther = await Fixture.GivenProductAsync(
            categoryId: otherCategory.Id,
            publishStatus: PublishStatus.Published);

        var queryResult = BrowseCatalogProductsQuery.Create(categoryId: rootCategory.Id);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().HaveCount(3);
        payload.Items.Should().Contain(p => p.Id == productInRoot.Id);
        payload.Items.Should().Contain(p => p.Id == productInChild.Id);
        payload.Items.Should().Contain(p => p.Id == productInGrandchild.Id);
        payload.Items.Should().NotContain(p => p.Id == productInOther.Id);
    }

    [Fact]
    public async Task BrowseCatalogProducts_ShouldFilterByFeaturedStatus()
    {
        var category = await Fixture.GivenCategoryAsync(publishStatus: PublishStatus.Published);

        var featuredProduct = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isFeatured: true,
            publishStatus: PublishStatus.Published);

        var notFeaturedProduct = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isFeatured: false,
            publishStatus: PublishStatus.Published);

        var queryResult = BrowseCatalogProductsQuery.Create(featured: true);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().HaveCount(1);
        payload.Items.Should().Contain(x => x.Id == featuredProduct.Id);
        payload.Items.Should().NotContain(x => x.Id == notFeaturedProduct.Id);
    }

    [Fact]
    public async Task BrowseCatalogProducts_ShouldCombineCategoryAndFeaturedFilters()
    {
        var category1 = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var category2 = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var featuredInCategory1 = await Fixture.GivenProductAsync(
            categoryId: category1.Id,
            isFeatured: true,
            publishStatus: PublishStatus.Published);

        var notFeaturedInCategory1 = await Fixture.GivenProductAsync(
            categoryId: category1.Id,
            isFeatured: false,
            publishStatus: PublishStatus.Published);

        var featuredInCategory2 = await Fixture.GivenProductAsync(
            categoryId: category2.Id,
            isFeatured: true,
            publishStatus: PublishStatus.Published);

        var queryResult = BrowseCatalogProductsQuery.Create(
            categoryId: category1.Id,
            featured: true);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().HaveCount(1);
        payload.Items.Should().Contain(x => x.Id == featuredInCategory1.Id);
        payload.Items.Should().NotContain(x => x.Id == notFeaturedInCategory1.Id);
        payload.Items.Should().NotContain(x => x.Id == featuredInCategory2.Id);
    }

    [Fact]
    public async Task BrowseCatalogProducts_ShouldExcludeDraftAndDeletedProducts()
    {
        var category = await Fixture.GivenCategoryAsync(publishStatus: PublishStatus.Published);

        var publishedProduct = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            publishStatus: PublishStatus.Published);

        var draftProduct = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            publishStatus: PublishStatus.Draft);

        var archivedProduct = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            publishStatus: PublishStatus.Published,
            isArchived: true);

        var queryResult = BrowseCatalogProductsQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().HaveCount(1);
        payload.Items.Should().Contain(x => x.Id == publishedProduct.Id);
        payload.Items.Should().NotContain(x => x.Id == draftProduct.Id);
        payload.Items.Should().NotContain(x => x.Id == archivedProduct.Id);
    }

    [Fact]
    public async Task BrowseCatalogProducts_ShouldExcludeProducts_WhenCategoryAncestorIsArchivedOrUnpublished()
    {
        var publishedRoot = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var publishedChild = await Fixture.GivenCategoryAsync(
            parentCategoryId: publishedRoot.Id,
            publishStatus: PublishStatus.Published);

        var draftRoot = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Draft);

        var publishedChildOfDraftRoot = await Fixture.GivenCategoryAsync(
            parentCategoryId: draftRoot.Id,
            publishStatus: PublishStatus.Published);

        var archivedRoot = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var publishedChildOfArchivedRoot = await Fixture.GivenCategoryAsync(
            parentCategoryId: archivedRoot.Id,
            publishStatus: PublishStatus.Published);

        var visibleProduct = await Fixture.GivenProductAsync(
            categoryId: publishedChild.Id,
            publishStatus: PublishStatus.Published);

        var hiddenByDraftAncestor = await Fixture.GivenProductAsync(
            categoryId: publishedChildOfDraftRoot.Id,
            publishStatus: PublishStatus.Published);

        var hiddenByArchivedAncestor = await Fixture.GivenProductAsync(
            categoryId: publishedChildOfArchivedRoot.Id,
            publishStatus: PublishStatus.Published);

        await Fixture.ArchiveExistingCategoryAsync(archivedRoot);

        var queryResult = BrowseCatalogProductsQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().HaveCount(1);
        payload.Items.Should().Contain(x => x.Id == visibleProduct.Id);
        payload.Items.Should().NotContain(x => x.Id == hiddenByDraftAncestor.Id);
        payload.Items.Should().NotContain(x => x.Id == hiddenByArchivedAncestor.Id);
    }

    [Fact]
    public async Task BrowseCatalogProducts_ShouldReturnCorrectImageAndAllProductFields()
    {
        var category = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var images = new[]
        {
            new CreateProductCommandImage(
                Source: "/images/secondary.jpg",
                AltText: "Secondary Image",
                IsPrimary: false,
                DisplayOrder: 2),
            new CreateProductCommandImage(
                Source: "/images/primary.jpg",
                AltText: "Primary Image",
                IsPrimary: true,
                DisplayOrder: 1)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"TestProduct-{TestToken}",
            slug: $"test-product-{TestToken}",
            price: 99.99m,
            metaDescription: "Test meta description",
            isFeatured: true,
            availabilityStatus: ProductAvailabilityStatus.Available,
            images: images,
            publishStatus: PublishStatus.Published);

        var queryResult = BrowseCatalogProductsQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Items.Should().HaveCount(1);
        var item = payload.Items[0];

        item.Id.Should().Be(product.Id);
        item.Name.Should().Be(product.Name.Value);
        item.Slug.Should().Be(product.CurrentSlug.Value);
        item.Price.Should().Be(99.99m);
        item.MetaDescription.Should().Be("Test meta description");
        item.CategoryId.Should().Be(category.Id);
        item.CategoryName.Should().Be(category.Name.Value);
        item.IsFeatured.Should().BeTrue();
        item.AvailabilityStatusCode.Should().Be(ProductAvailabilityStatus.Available.Code);

        item.ImageSource.Should().Be("/images/primary.jpg");
        item.ImageAltText.Should().Be("Primary Image");
    }
}
