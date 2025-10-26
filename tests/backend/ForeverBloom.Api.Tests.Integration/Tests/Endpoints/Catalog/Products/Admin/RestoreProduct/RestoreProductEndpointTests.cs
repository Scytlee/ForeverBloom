using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.RestoreProduct;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Validation;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.RestoreProduct;

public sealed class RestoreProductEndpointTests : BackendAppTestClassBase
{
    private static string EndpointUrl(int productId) => $"/api/v1/admin/products/{productId}/restore";

    public RestoreProductEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task RestoreProductEndpoint_ShouldRespondWith200Ok_WhenRestoreIsSuccessful()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Create a product
        var createdProduct = await client.CreateProductAsync(
          categoryId: createdCategory.Id,
          name: $"Restore-{_testId:N}"[..20],
          slug: $"restore-{_testId:N}"[..20],
          price: 99.99m,
          displayOrder: 5,
          publishStatus: PublishStatus.Published,
          availability: ProductAvailabilityStatus.Available,
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Archive the product first
        var archivedProduct = await client.ArchiveProductAsync(createdProduct.Id, createdProduct.RowVersion, TestContext.Current.CancellationToken);

        // Act: Restore the product
        var restoreRequest = new RestoreProductRequest
        {
            RowVersion = archivedProduct.RowVersion
        };
        var response = await client.PostAsJsonAsync(EndpointUrl(createdProduct.Id), restoreRequest, TestContext.Current.CancellationToken);

        // Assert response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<RestoreProductResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        // Assert database state
        var persistedProduct = await DbContext.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.Should().NotBeNull();
        persistedProduct.DeletedAt.Should().BeNull();
        persistedProduct.RowVersion.Should().BeGreaterThan(archivedProduct.RowVersion);

        // Verify response content
        responseContent.DeletedAt.Should().BeNull();
        responseContent.RowVersion.Should().Be(persistedProduct.RowVersion);

        // Verify product is accessible via normal queries (not soft-deleted)
        var publicProduct = await DbContext.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        publicProduct.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreProductEndpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsMissing()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category and product
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        var createdProduct = await client.CreateProductAsync(
          categoryId: createdCategory.Id,
          name: $"Product-{_testId:N}"[..20],
          slug: $"prod-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Archive the product
        await client.ArchiveProductAsync(createdProduct.Id, createdProduct.RowVersion, TestContext.Current.CancellationToken);

        // Act: Try to restore with invalid RowVersion
        var restoreRequest = new RestoreProductRequest
        {
            RowVersion = 0
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(createdProduct.Id), restoreRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(RestoreProductRequest.RowVersion), ProductValidation.ErrorCodes.RowVersionRequired);

        // Verify database unchanged
        var persistedProduct = await DbContext.Products.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.DeletedAt.Should().NotBeNull(); // Should remain archived
    }

    [Fact]
    public async Task RestoreProductEndpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Act: Try to restore non-existent product
        var restoreRequest = new RestoreProductRequest
        {
            RowVersion = 1
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(999999), restoreRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreProductEndpoint_ShouldRespondWith409Conflict_WhenConcurrentUpdateOccurs()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category and product
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        var createdProduct = await client.CreateProductAsync(
          categoryId: createdCategory.Id,
          name: $"Conflict-{_testId:N}"[..20],
          slug: $"conflict-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Archive the product
        var archivedProduct = await client.ArchiveProductAsync(createdProduct.Id, createdProduct.RowVersion, TestContext.Current.CancellationToken);

        // Arrange: Simulate concurrent update by modifying the product directly in the database
        var product = await DbContext.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        product.Name = "Concurrently Modified";
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: Try to restore with correct row version but concurrent modification occurred
        var restoreRequest = new RestoreProductRequest
        {
            RowVersion = archivedProduct.RowVersion
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(createdProduct.Id), restoreRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Verify database state unchanged by restore attempt
        var persistedProduct = await DbContext.Products.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.Name.Should().Be("Concurrently Modified");
        persistedProduct.DeletedAt.Should().NotBeNull();
    }
}
