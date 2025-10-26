using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProductImages;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Helpers.Validation;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.UpdateProductImages;

public sealed class UpdateProductImagesEndpointTests : BackendAppTestClassBase
{
    private static string EndpointUrl(int productId) => $"/api/v1/admin/products/{productId}/images";

    public UpdateProductImagesEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task UpdateProductImagesEndpoint_ShouldRespondWith200Ok_WhenUpdateIsSuccessful()
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
          name: $"Product-{_testId:N}"[..20],
          slug: $"prod-{_testId:N}"[..20],
          price: 99.99m,
          displayOrder: 5,
          isFeatured: false,
          publishStatus: PublishStatus.Draft,
          availability: ProductAvailabilityStatus.Available,
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Define initial images collection
        var initialImages = new List<UpdateProductImageItem>
    {
      new() { ImagePath = "/images/initial1.jpg", IsPrimary = true, DisplayOrder = 0, AltText = "Initial Primary" },
      new() { ImagePath = "/images/initial2.jpg", IsPrimary = false, DisplayOrder = 1, AltText = "Initial Secondary" }
    };
        var initialImagesResponse = await client.UpdateProductImagesAsync(
          createdProduct.Id,
          createdProduct.RowVersion,
          images: initialImages,
          cancellationToken: TestContext.Current.CancellationToken);

        // Act: Update images with new collection
        var newImages = new List<UpdateProductImageItem>
    {
      new() { ImagePath = "/images/new1.jpg", IsPrimary = false, DisplayOrder = 0, AltText = "New Image 1" },
      new() { ImagePath = "/images/new2.jpg", IsPrimary = true, DisplayOrder = 1, AltText = "New Primary" },
      new() { ImagePath = "/images/new3.jpg", IsPrimary = false, DisplayOrder = 2, AltText = "New Image 3" }
    };
        var updateRequest = new UpdateProductImagesRequest
        {
            Images = newImages,
            RowVersion = initialImagesResponse.RowVersion
        };

        var response = await client.PutAsJsonAsync(EndpointUrl(createdProduct.Id), updateRequest, TestContext.Current.CancellationToken);

        // Assert response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<UpdateProductImagesResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        // Assert response echoes order and increments RowVersion
        responseContent.Images.Should().HaveCount(newImages.Count);
        responseContent.Images.Should().BeInAscendingOrder(i => i.DisplayOrder);
        responseContent.RowVersion.Should().BeGreaterThan(initialImagesResponse.RowVersion);

        // Assert database state
        var persistedProduct = await DbContext.Products
          .Include(p => p.Images)
          .AsNoTracking()
          .SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);

        persistedProduct.RowVersion.Should().Be(responseContent.RowVersion);
        persistedProduct.UpdatedAt.Should().BeAfter(persistedProduct.CreatedAt);
        persistedProduct.Images.Should().HaveCount(newImages.Count);

        // Verify exactly one primary image exists
        persistedProduct.Images.Count(i => i.IsPrimary).Should().Be(1);

        // Verify database images match update request and are ordered correctly
        var orderedImages = persistedProduct.Images.OrderBy(i => i.DisplayOrder).ToList();
        var orderedRequestImages = newImages.OrderBy(i => i.DisplayOrder).ToList();

        for (int i = 0; i < orderedImages.Count; i++)
        {
            var persistedImage = orderedImages[i];
            var requestImage = orderedRequestImages[i];

            // Assert request item maps to persisted entity
            AssertionHelpers.AssertAllPropertiesAreMapped(
              sourceObject: requestImage,
              destinationObject: persistedImage);
        }

        // Verify response images match database entities
        var orderedResponseImages = responseContent.Images.OrderBy(i => i.DisplayOrder).ToList();

        for (int i = 0; i < orderedImages.Count; i++)
        {
            var persistedImage = orderedImages[i];
            var responseImage = orderedResponseImages[i];

            // Assert persisted entity maps to response item
            AssertionHelpers.AssertAllPropertiesAreMapped(
              sourceObject: persistedImage,
              destinationObject: responseImage,
              sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
              {
          nameof(persistedImage.Id),
          nameof(persistedImage.ProductId),
          nameof(persistedImage.Product)
              });
        }
    }

    [Fact]
    public async Task UpdateProductImagesEndpoint_ShouldRespondWith400BadRequest_WhenExactlyOnePrimaryImageRequired()
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

        // Act: Try to update with multiple primary images (violates FluentValidation rule)
        var updateRequest = new UpdateProductImagesRequest
        {
            Images = new List<UpdateProductImageItem>
      {
        new() { ImagePath = "/images/img1.jpg", IsPrimary = true, DisplayOrder = 0, AltText = "Primary 1" },
        new() { ImagePath = "/images/img2.jpg", IsPrimary = true, DisplayOrder = 1, AltText = "Primary 2" }
      },
            RowVersion = createdProduct.RowVersion
        };

        var response = await client.PutAsJsonAsync(EndpointUrl(createdProduct.Id), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(UpdateProductImagesRequest.Images), ProductValidation.ErrorCodes.ExactlyOnePrimaryImage);

        // Verify database unchanged
        var persistedProduct = await DbContext.Products
          .Include(p => p.Images)
          .AsNoTracking()
          .SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.RowVersion.Should().Be(createdProduct.RowVersion);
        persistedProduct.Images.Should().BeEmpty(); // Product should still have no images
    }

    [Fact]
    public async Task UpdateProductImagesEndpoint_ShouldRespondWith404NotFound_WhenProductNotFound()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Act: Try to update images for non-existent product
        var updateRequest = new UpdateProductImagesRequest
        {
            Images = new List<UpdateProductImageItem>
      {
        new() { ImagePath = "/images/img1.jpg", IsPrimary = true, DisplayOrder = 0, AltText = "Primary" }
      },
            RowVersion = 1
        };

        var response = await client.PutAsJsonAsync(EndpointUrl(999999), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProductImagesEndpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
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

        // Arrange: Simulate concurrent update by modifying the product directly in the database
        var product = await DbContext.Products.SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        product.Name = "Concurrently Modified";
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: Try to update images with stale RowVersion (optimistic concurrency check)
        var updateRequest = new UpdateProductImagesRequest
        {
            Images = new List<UpdateProductImageItem>
      {
        new() { ImagePath = "/images/img1.jpg", IsPrimary = true, DisplayOrder = 0, AltText = "Primary" }
      },
            RowVersion = createdProduct.RowVersion
        };

        var response = await client.PutAsJsonAsync(EndpointUrl(createdProduct.Id), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Verify database unchanged by images update attempt
        var persistedProduct = await DbContext.Products
          .Include(p => p.Images)
          .AsNoTracking()
          .SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.Name.Should().Be("Concurrently Modified"); // Should still have concurrent modification
        persistedProduct.Images.Should().BeEmpty(); // Should still have no images
    }
}
