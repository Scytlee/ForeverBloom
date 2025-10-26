using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.GetAdminProductById;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProductImages;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.GetAdminProductById;

public sealed class GetAdminProductByIdEndpointTests : BackendAppTestClassBase
{
    private const string EndpointBaseUrl = "/api/v1/admin/products";

    public GetAdminProductByIdEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task GetAdminProductByIdEndpoint_ShouldRespondWith200Ok_AndProductDetails_WhenIdExists()
    {
        BuildApp();

        var arrangeClient = _app.RequestClient().UseAdminKey();

        var category = await arrangeClient.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var created = await arrangeClient.CreateProductAsync(
          categoryId: category.Id,
          name: $"Product-{_testId:N}"[..20],
          slug: $"prod-{_testId:N}"[..20],
          seoTitle: "Test Product SEO Title",
          fullDescription: "This is a comprehensive test product description",
          metaDescription: "Test product meta description",
          price: 299.99m,
          displayOrder: 5,
          isFeatured: true,
          publishStatus: PublishStatus.Published,
          availability: ProductAvailabilityStatus.Available,
          cancellationToken: TestContext.Current.CancellationToken);

        await arrangeClient.UpdateProductImagesAsync(
          created.Id,
          created.RowVersion,
          images: [
            new UpdateProductImageItem
        {
          ImagePath = "/images/test-product-primary.jpg",
          IsPrimary = true,
          DisplayOrder = 1,
          AltText = "Primary product image"
        },
        new UpdateProductImageItem
        {
          ImagePath = "/images/test-product-secondary.jpg",
          IsPrimary = false,
          DisplayOrder = 2,
          AltText = "Secondary product image"
        }
          ],
          cancellationToken: TestContext.Current.CancellationToken);

        var client = _app.RequestClient().UseAdminKey();
        var response = await client.GetAsync($"{EndpointBaseUrl}/{created.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetAdminProductByIdResponse>(TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();

        // Use domain information to verify response correctness
        var persistedProduct = await DbContext.Products
          .AsNoTracking()
          .Include(p => p.Category)
          .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
          .FirstOrDefaultAsync(p => p.Id == created.Id, TestContext.Current.CancellationToken);
        persistedProduct.Should().NotBeNull();

        // Verify product entity to response mapping using assertion helper
        AssertionHelpers.AssertAllPropertiesAreMapped(
          sourceObject: persistedProduct,
          destinationObject: payload,
          overridesMap: new(StringComparer.OrdinalIgnoreCase)
          {
        { nameof(persistedProduct.CurrentSlug), nameof(payload.Slug) }
          },
          sourceExcludes: new(StringComparer.OrdinalIgnoreCase)
          {
        nameof(persistedProduct.Category),
        nameof(persistedProduct.Images)
          },
          equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        // Verify specific business logic and relationships
        payload.CategoryId.Should().Be(persistedProduct.CategoryId);
        payload.CategoryName.Should().Be(persistedProduct.Category.Name);
        payload.CategorySlug.Should().Be(persistedProduct.Category.CurrentSlug);

        // Verify image mapping and ordering
        payload.Images.Should().HaveCount(persistedProduct.Images.Count);
        payload.Images.Should().ContainSingle(i => i.IsPrimary);
        var persistedImagesOrdered = persistedProduct.Images.OrderBy(i => i.DisplayOrder).ToList();
        for (int i = 0; i < persistedImagesOrdered.Count; i++)
        {
            var persistedImage = persistedImagesOrdered[i];
            var responseImage = payload.Images.ElementAt(i);
            responseImage.ImagePath.Should().Be(persistedImage.ImagePath);
            responseImage.IsPrimary.Should().Be(persistedImage.IsPrimary);
            responseImage.AltText.Should().Be(persistedImage.AltText);
            responseImage.DisplayOrder.Should().Be(persistedImage.DisplayOrder);
        }
    }

    [Fact]
    public async Task GetAdminProductByIdEndpoint_ShouldRespondWith404NotFound_WhenIdDoesNotExist()
    {
        BuildApp();

        var client = _app.RequestClient().UseAdminKey();

        var response = await client.GetAsync($"{EndpointBaseUrl}/999999", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
