using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Application.Products.Queries.GetProductById;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;

namespace ForeverBloom.Application.IntegrationTests.Products.Queries;

public sealed class GetProductByIdUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task GetProductById_ShouldReturnMinimalProduct_WhenOnlyRequiredFieldsProvided()
    {
        var name = $"Minimal-{TestToken}";
        var slug = $"minimal-{TestToken}";

        var category = await Fixture.GivenCategoryAsync();
        var creationTimestamp = Fixture.TimeProvider.CurrentTime;
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: name,
            slug: slug,
            creationTimestamp: creationTimestamp);

        var queryResult = GetProductByIdQuery.Create(product.Id);
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
        payload.PublishStatusCode.Should().Be(PublishStatus.Draft.Code);
        payload.AvailabilityStatusCode.Should().Be(ProductAvailabilityStatus.ComingSoon.Code);
        payload.Images.Should().BeEmpty();
        payload.CreatedAt.Should().Be(creationTimestamp);
        payload.UpdatedAt.Should().Be(creationTimestamp);
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().Be(product.RowVersion);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnFullProduct_WithAllOptionalFieldsAndImages()
    {
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

        var category = await Fixture.GivenCategoryAsync();
        var creationTimestamp = Fixture.TimeProvider.CurrentTime;
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
            creationTimestamp: creationTimestamp);

        var queryResult = GetProductByIdQuery.Create(product.Id);
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
        payload.CategoryId.Should().Be(product.CategoryId);
        payload.Price.Should().Be(price);
        payload.IsFeatured.Should().BeTrue();
        payload.PublishStatusCode.Should().Be(PublishStatus.Draft.Code);
        payload.AvailabilityStatusCode.Should().Be(ProductAvailabilityStatus.Available.Code);
        payload.CreatedAt.Should().Be(creationTimestamp);
        payload.UpdatedAt.Should().Be(creationTimestamp);
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().Be(product.RowVersion);

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
    public async Task GetProductById_ShouldReturnProduct_WhenCategoryIsArchived()
    {
        var category = await Fixture.GivenCategoryAsync();

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id);

        await Fixture.ArchiveExistingCategoryAsync(category);

        var queryResult = GetProductByIdQuery.Create(product.Id);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Id.Should().Be(product.Id);
        payload.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnProduct_WhenCategoryHasArchivedAncestor()
    {
        var parentCategory = await Fixture.GivenCategoryAsync();

        var childCategory = await Fixture.GivenCategoryAsync(
            parentCategoryId: parentCategory.Id);

        var product = await Fixture.GivenProductAsync(
            categoryId: childCategory.Id);

        await Fixture.ArchiveExistingCategoryAsync(parentCategory);

        var queryResult = GetProductByIdQuery.Create(product.Id);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Id.Should().Be(product.Id);
        payload.CategoryId.Should().Be(childCategory.Id);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        const long missingProductId = 999_999L;

        var queryResult = GetProductByIdQuery.Create(missingProductId);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundById>();
        error.Id.Should().Be(missingProductId);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnFailure_WhenProductIsArchived()
    {
        var category = await Fixture.GivenCategoryAsync();
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);

        var queryResult = GetProductByIdQuery.Create(product.Id);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundById>();
        error.Id.Should().Be(product.Id);
    }
}
