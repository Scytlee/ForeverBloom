using System.Net;
using FluentAssertions;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Validation;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.DeleteProduct;

public sealed class DeleteProductEndpointTests : BackendAppTestClassBase
{
    public DeleteProductEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    private static string GetEndpointUrl(int productId, uint? rowVersion = null)
    {
        return $"/api/v1/admin/products/{productId}{(rowVersion.HasValue ? $"?RowVersion={rowVersion.Value}" : "")}";
    }

    [Fact]
    public async Task DeleteProductEndpoint_ShouldRespondWith204NoContent_WhenRequestIsValid()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create and archive a product
        var category = await client.CreateCategoryAsync(
            name: $"Test Category {_testId:N}"[..20],
            slug: $"test-cat-{_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);

        var product = await client.CreateProductAsync(
            categoryId: category.Id,
            name: $"Test Product {_testId:N}"[..20],
            slug: $"test-prod-{_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);
        var archivedProduct = await client.ArchiveProductAsync(product.Id, product.RowVersion, TestContext.Current.CancellationToken);

        // Act: Delete the archived product
        var response = await client.DeleteAsync(GetEndpointUrl(product.Id, archivedProduct.RowVersion), TestContext.Current.CancellationToken);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert database state: product should be hard deleted
        var deletedProduct = await DbContext.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == product.Id, TestContext.Current.CancellationToken);
        deletedProduct.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProductEndpoint_ShouldRespondWith400BadRequest_WhenProductNotArchived()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a product but don't archive it
        var category = await client.CreateCategoryAsync(
            name: $"Test Category {_testId:N}"[..20],
            slug: $"test-cat-{_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);

        var product = await client.CreateProductAsync(
            categoryId: category.Id,
            name: $"Test Product {_testId:N}"[..20],
            slug: $"test-prod-{_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);

        // Act: Attempt to delete the non-archived product
        var response = await client.DeleteAsync(GetEndpointUrl(product.Id, product.RowVersion), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty("productId", ProductValidation.ErrorCodes.ProductNotArchived);

        // Verify product still exists in database
        var existingProduct = await DbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == product.Id, TestContext.Current.CancellationToken);
        existingProduct.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteProductEndpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Act: Attempt to delete a non-existent product
        const int nonExistentId = 999999;
        var response = await client.DeleteAsync(GetEndpointUrl(nonExistentId, 1), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProductEndpoint_ShouldRespondWith409Conflict_WhenConcurrencyConflictOccurs()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create and archive a product
        var category = await client.CreateCategoryAsync(
            name: $"Test Category {_testId:N}"[..20],
            slug: $"test-cat-{_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);

        var product = await client.CreateProductAsync(
            categoryId: category.Id,
            name: $"Test Product {_testId:N}"[..20],
            slug: $"test-prod-{_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);
        var archivedProduct = await client.ArchiveProductAsync(product.Id, product.RowVersion, TestContext.Current.CancellationToken);

        // Simulate concurrent modification by updating the product
        // Update endpoint does not handle archived entities as of now, so it must be updated via database
        var productInDb = await DbContext.Products
            .IgnoreQueryFilters()
            .SingleAsync(p => p.Id == product.Id, TestContext.Current.CancellationToken);
        productInDb.Name = "Modified Name";
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        // Act: Attempt to delete with stale row version
        var response = await client.DeleteAsync(GetEndpointUrl(product.Id, archivedProduct.RowVersion), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Verify product still exists in database
        var existingProduct = await DbContext.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == product.Id, TestContext.Current.CancellationToken);
        existingProduct.Should().NotBeNull();
    }
}
