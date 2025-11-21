using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Application.Products.Queries.GetProductBySlug;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;

namespace ForeverBloom.Application.IntegrationTests.Products.Queries;

public sealed class GetProductBySlugUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task GetProductBySlug_ShouldReturnMinimalPublishedProduct_WhenOnlyRequiredFieldsProvided()
    {
        var category = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var name = $"Minimal-{TestToken}";
        var slug = $"minimal-{TestToken}";

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: name,
            slug: slug,
            publishStatus: PublishStatus.Published);

        var queryResult = GetProductBySlugQuery.Create(slug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Id.Should().Be(product.Id);
        payload.Name.Should().Be(name);
        payload.SeoTitle.Should().BeNull();
        payload.FullDescription.Should().BeNull();
        payload.MetaDescription.Should().BeNull();
        payload.Slug.Should().Be(slug);
        payload.CategoryId.Should().Be(product.CategoryId);
        payload.Price.Should().BeNull();
        payload.IsFeatured.Should().BeFalse();
        payload.AvailabilityStatusCode.Should().Be(ProductAvailabilityStatus.ComingSoon.Code);
        payload.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnFullPublishedProduct_WithAllOptionalFieldsAndOrderedImages()
    {
        var category = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var name = $"Deluxe-{TestToken}";
        var slug = $"deluxe-{TestToken}";
        var seoTitle = $"Premium Bouquet {TestToken}";
        var fullDescription = $"<p>Hand-crafted arrangement {TestToken}</p>";
        var metaDescription = $"Exquisite floral design {TestToken}";
        const decimal price = 299.99m;

        var images = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-secondary.jpg", $"Secondary {TestToken}", false, 2),
            new CreateProductCommandImage($"/images/{TestToken}-primary.jpg", $"Primary {TestToken}", true, 1),
            new CreateProductCommandImage($"/images/{TestToken}-tertiary.jpg", $"Tertiary {TestToken}", false, 3)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: name,
            slug: slug,
            seoTitle: seoTitle,
            fullDescription: fullDescription,
            metaDescription: metaDescription,
            price: price,
            isFeatured: true,
            availabilityStatus: ProductAvailabilityStatus.Available,
            images: images,
            publishStatus: PublishStatus.Published);

        var queryResult = GetProductBySlugQuery.Create(slug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Id.Should().Be(product.Id);
        payload.Name.Should().Be(name);
        payload.SeoTitle.Should().Be(seoTitle);
        payload.FullDescription.Should().Be(fullDescription);
        payload.MetaDescription.Should().Be(metaDescription);
        payload.Slug.Should().Be(slug);
        payload.CategoryId.Should().Be(category.Id);
        payload.CategoryName.Should().Be(category.Name.Value);
        payload.Price.Should().Be(price);
        payload.IsFeatured.Should().BeTrue();
        payload.AvailabilityStatusCode.Should().Be(ProductAvailabilityStatus.Available.Code);

        // Verify images are returned in display order
        payload.Images.Should().HaveCount(3);

        var expectedOrder = images.OrderBy(i => i.DisplayOrder).ToArray();
        for (var index = 0; index < expectedOrder.Length; index++)
        {
            var expected = expectedOrder[index];
            var actual = payload.Images[index];

            actual.ImagePath.Should().Be(expected.Source);
            actual.AltText.Should().Be(expected.AltText);
            actual.IsPrimary.Should().Be(expected.IsPrimary);
            actual.DisplayOrder.Should().Be(expected.DisplayOrder);
        }
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnFailure_WhenSlugDoesNotExist()
    {
        var missingSlug = $"missing-{TestToken}";

        var queryResult = GetProductBySlugQuery.Create(missingSlug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundBySlug>();
        error.Slug.Should().Be(missingSlug);
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnSlugChanged_WhenHistoricalSlugProvided()
    {
        var category = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var originalSlug = $"nightshade-{TestToken}";
        var newSlug = $"nightshade-{TestToken}-v2";

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: newSlug,
            publishStatus: PublishStatus.Published,
            slugHistory: [originalSlug]);

        var queryResult = GetProductBySlugQuery.Create(originalSlug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.SlugChanged>();
        error.AttemptedSlug.Should().Be(originalSlug);
        error.CurrentSlug.Should().Be(newSlug);
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnFailure_WhenProductIsNotPublished()
    {
        var category = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var slug = $"unpublished-{TestToken}";

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: slug,
            publishStatus: PublishStatus.Draft);

        var queryResult = GetProductBySlugQuery.Create(slug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundBySlug>();
        error.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnFailure_WhenProductCategoryIsArchived()
    {
        var category = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var slug = $"orphaned-{TestToken}";

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Orphaned-{TestToken}",
            slug: slug,
            publishStatus: PublishStatus.Published);

        await Fixture.ArchiveExistingCategoryAsync(category);

        var queryResult = GetProductBySlugQuery.Create(slug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundBySlug>();
        error.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnFailure_WhenProductCategoryHasArchivedAncestor()
    {
        var rootCategory = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var childCategory = await Fixture.GivenCategoryAsync(
            parentCategoryId: rootCategory.Id,
            publishStatus: PublishStatus.Published);

        var slug = $"twig-{TestToken}";

        await Fixture.GivenProductAsync(
            categoryId: childCategory.Id,
            slug: slug,
            publishStatus: PublishStatus.Published);

        await Fixture.ArchiveExistingCategoryAsync(rootCategory);

        var queryResult = GetProductBySlugQuery.Create(slug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundBySlug>();
        error.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnFailure_WhenProductIsArchived()
    {
        var category = await Fixture.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var slug = $"archived-{TestToken}";

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: slug,
            publishStatus: PublishStatus.Published,
            isArchived: true);

        var queryResult = GetProductBySlugQuery.Create(slug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundBySlug>();
        error.Slug.Should().Be(slug);
    }
}
