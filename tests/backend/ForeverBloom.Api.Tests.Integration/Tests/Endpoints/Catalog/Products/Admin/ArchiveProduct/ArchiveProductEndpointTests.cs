using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.ArchiveProduct;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Validation;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.ArchiveProduct;

public sealed class ArchiveProductEndpointTests : BackendAppTestClassBase
{
    private static string EndpointUrl(int productId) => $"/api/v1/admin/products/{productId}/archive";

    public ArchiveProductEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task ArchiveProductEndpoint_ShouldRespondWith200Ok_WhenArchiveIsSuccessful()
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
          name: $"Archive-{_testId:N}"[..20],
          slug: $"archive-{_testId:N}"[..20],
          price: 99.99m,
          displayOrder: 5,
          isFeatured: false,
          publishStatus: PublishStatus.Published,
          availability: ProductAvailabilityStatus.Available,
          cancellationToken: TestContext.Current.CancellationToken);

        // Act: Archive the product
        var lowerBound = TimeProvider.System.GetUtcNow();
        var archiveRequest = new ArchiveProductRequest
        {
            RowVersion = createdProduct.RowVersion
        };
        var response = await client.PostAsJsonAsync(EndpointUrl(createdProduct.Id), archiveRequest, TestContext.Current.CancellationToken);
        var upperBound = TimeProvider.System.GetUtcNow();

        // Assert response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<ArchiveProductResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        // Assert database state
        var persistedProduct = await DbContext.Products.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.Should().NotBeNull();
        persistedProduct.DeletedAt.Should().BeOnOrAfter(lowerBound);
        persistedProduct.DeletedAt.Should().BeOnOrBefore(upperBound);
        persistedProduct.RowVersion.Should().BeGreaterThan(createdProduct.RowVersion);

        // Verify response content
        responseContent.DeletedAt.Should().BeCloseTo(persistedProduct.DeletedAt.Value, TemporalTolerances.DatabaseTimestamp);
        responseContent.RowVersion.Should().Be(persistedProduct.RowVersion);

        // Verify product is soft-deleted (not accessible via normal queries)
        var publicProduct = await DbContext.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        publicProduct.Should().BeNull();
    }

    [Fact]
    public async Task ArchiveProductEndpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsMissing()
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

        // Act: Try to archive with invalid RowVersion
        var archiveRequest = new ArchiveProductRequest
        {
            RowVersion = 0
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(createdProduct.Id), archiveRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(ArchiveProductRequest.RowVersion), ProductValidation.ErrorCodes.RowVersionRequired);

        // Verify database unchanged
        var persistedProduct = await DbContext.Products.AsNoTracking().SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.DeletedAt.Should().BeNull(); // Should not be archived
        persistedProduct.RowVersion.Should().Be(createdProduct.RowVersion); // Should not be incremented
    }

    [Fact]
    public async Task ArchiveProductEndpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Act: Try to archive non-existent product
        var archiveRequest = new ArchiveProductRequest
        {
            RowVersion = 1
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(999999), archiveRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ArchiveProductEndpoint_ShouldRespondWith409Conflict_WhenConcurrentUpdateOccurs()
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

        // Arrange: Simulate concurrent update by modifying the product directly in the database
        var product = await DbContext.Products.SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        product.Name = "Concurrently Modified";
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: Try to archive with stale row version
        var archiveRequest = new ArchiveProductRequest
        {
            RowVersion = createdProduct.RowVersion
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(createdProduct.Id), archiveRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Verify database unchanged by archive attempt
        var persistedProduct = await DbContext.Products.AsNoTracking().SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.Name.Should().Be("Concurrently Modified");
        persistedProduct.DeletedAt.Should().BeNull();
    }
}
