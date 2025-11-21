using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Application.Products.Queries.ListProducts;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;

namespace ForeverBloom.Application.IntegrationTests.Products.Queries;

public sealed class ListProductsUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task ListProducts_ShouldReturnPagedResults_WithDefaultPagination()
    {
        var category = await Fixture.GivenCategoryAsync();
        var product1 = await Fixture.GivenProductAsync(categoryId: category.Id,
            images:
            [
                new CreateProductCommandImage("/images/product1.jpg", "Product 1", true, 1)
            ]);
        var product2 = await Fixture.GivenProductAsync(categoryId: category.Id);
        var product3 = await Fixture.GivenProductAsync(categoryId: category.Id);

        var queryResult = ListProductsQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(3);
        payload.PageNumber.Should().Be(PaginationConstants.DefaultPageNumber);
        payload.PageSize.Should().Be(PaginationConstants.DefaultPageSize);

        payload.Items.Should().Contain(p => p.Id == product1.Id);
        payload.Items.Should().Contain(p => p.Id == product2.Id);
        payload.Items.Should().Contain(p => p.Id == product3.Id);

        // Verify image data is returned for product with images
        var product1Item = payload.Items.Single(p => p.Id == product1.Id);
        product1Item.ImageSource.Should().Be(product1.Images.Single().Image.Source.Value);
        product1Item.ImageAltText.Should().Be(product1.Images.Single().Image.AltText);
    }

    [Fact]
    public async Task ListProducts_ShouldReturnEmptyList_WhenNoProductsExist()
    {
        var queryResult = ListProductsQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(0);
        payload.PageNumber.Should().Be(PaginationConstants.DefaultPageNumber);
        payload.PageSize.Should().Be(PaginationConstants.DefaultPageSize);
    }

    [Fact]
    public async Task ListProducts_ShouldApplySorting()
    {
        var category = await Fixture.GivenCategoryAsync();

        var productC = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Charlie-{TestToken}");

        var productA = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Alpha-{TestToken}");

        var productB = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Bravo-{TestToken}");

        var queryResult = ListProductsQuery.Create(sortBy: "name:asc");
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(3);
        payload.Items[0].Id.Should().Be(productA.Id);
        payload.Items[1].Id.Should().Be(productB.Id);
        payload.Items[2].Id.Should().Be(productC.Id);
    }

    [Fact]
    public async Task ListProducts_ShouldFilterBySearchTerm()
    {
        var category = await Fixture.GivenCategoryAsync();

        var matchingProduct1 = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Roses-{TestToken}",
            fullDescription: "Beautiful flowers");

        var matchingProduct2 = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Tulips-{TestToken}",
            metaDescription: $"Spring roses collection {TestToken}");

        var nonMatchingProduct = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Orchids-{TestToken}",
            fullDescription: "Exotic plants");

        var queryResult = ListProductsQuery.Create(searchTerm: "roses");
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(2);
        payload.Items.Should().Contain(p => p.Id == matchingProduct1.Id);
        payload.Items.Should().Contain(p => p.Id == matchingProduct2.Id);
        payload.Items.Should().NotContain(p => p.Id == nonMatchingProduct.Id);
    }

    [Fact]
    public async Task ListProducts_ShouldFilterByCategory_DirectOnly()
    {
        // Arranged hierarchy:
        //   parent -> child1 -> grandchild
        //          -> child2
        var parent = await Fixture.GivenCategoryAsync();

        var child1 = await Fixture.GivenCategoryAsync(
            parentCategoryId: parent.Id);

        var child2 = await Fixture.GivenCategoryAsync(
            parentCategoryId: parent.Id);

        var grandchild = await Fixture.GivenCategoryAsync(
            parentCategoryId: child1.Id);

        var productInChild1A = await Fixture.GivenProductAsync(categoryId: child1.Id);
        var productInChild1B = await Fixture.GivenProductAsync(categoryId: child1.Id);
        var productInChild2 = await Fixture.GivenProductAsync(categoryId: child2.Id);
        var productInGrandchild = await Fixture.GivenProductAsync(categoryId: grandchild.Id);
        var productInParent = await Fixture.GivenProductAsync(categoryId: parent.Id);

        var queryResult = ListProductsQuery.Create(
            categoryId: child1.Id,
            includeSubcategories: false);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(2);
        payload.Items.Should().Contain(p => p.Id == productInChild1A.Id);
        payload.Items.Should().Contain(p => p.Id == productInChild1B.Id);
        payload.Items.Should().NotContain(p => p.Id == productInChild2.Id);
        payload.Items.Should().NotContain(p => p.Id == productInGrandchild.Id);
        payload.Items.Should().NotContain(p => p.Id == productInParent.Id);
    }

    [Fact]
    public async Task ListProducts_ShouldFilterByCategory_WithSubcategories()
    {
        // Arranged hierarchy:
        //   parent -> child -> grandchild
        //   otherRoot
        var parent = await Fixture.GivenCategoryAsync();

        var child = await Fixture.GivenCategoryAsync(
            parentCategoryId: parent.Id);

        var grandchild = await Fixture.GivenCategoryAsync(
            parentCategoryId: child.Id);

        var otherRoot = await Fixture.GivenCategoryAsync();

        var productInParent = await Fixture.GivenProductAsync(categoryId: parent.Id);
        var productInChild = await Fixture.GivenProductAsync(categoryId: child.Id);
        var productInGrandchild = await Fixture.GivenProductAsync(categoryId: grandchild.Id);
        var productInOtherRoot = await Fixture.GivenProductAsync(categoryId: otherRoot.Id);

        var queryResult = ListProductsQuery.Create(
            categoryId: parent.Id,
            includeSubcategories: true);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(3);
        payload.Items.Should().Contain(p => p.Id == productInParent.Id);
        payload.Items.Should().Contain(p => p.Id == productInChild.Id);
        payload.Items.Should().Contain(p => p.Id == productInGrandchild.Id);
        payload.Items.Should().NotContain(p => p.Id == productInOtherRoot.Id);
    }

    [Fact]
    public async Task ListProducts_ShouldExcludeSoftDeletedProducts()
    {
        var category = await Fixture.GivenCategoryAsync();

        var activeProduct = await Fixture.GivenProductAsync(categoryId: category.Id);

        var deletedProduct = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);

        var queryResult = ListProductsQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(1);
        payload.Items.Should().Contain(p => p.Id == activeProduct.Id);
        payload.Items.Should().NotContain(p => p.Id == deletedProduct.Id);
    }

    [Fact]
    public async Task ListProducts_ShouldIncludePrimaryImage_WhenProductHasImages()
    {
        var category = await Fixture.GivenCategoryAsync();

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            images:
            [
                new CreateProductCommandImage("/images/img1.jpg", "Image 1", false, 1),
                new CreateProductCommandImage("/images/img2.jpg", "Image 2", true, 2),
                new CreateProductCommandImage("/images/img3.jpg", "Image 3", false, 3)
            ]);

        var queryResult = ListProductsQuery.Create();
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.TotalCount.Should().Be(1);
        var productItem = payload.Items.Single();
        productItem.Id.Should().Be(product.Id);
        productItem.ImageSource.Should().Be(product.Images.Single(i => i.IsPrimary).Image.Source.Value);
        productItem.ImageAltText.Should().Be(product.Images.Single(i => i.IsPrimary).Image.AltText);
    }
}
