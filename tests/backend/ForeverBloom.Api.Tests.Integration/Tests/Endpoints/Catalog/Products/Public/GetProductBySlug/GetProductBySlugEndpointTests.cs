using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProductImages;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductBySlug;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Public.GetProductBySlug;

public sealed class GetProductBySlugEndpointTests : BackendAppTestClassBase
{
    private const string EndpointBaseUrl = "/api/v1/products";

    public GetProductBySlugEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task GetProductBySlugEndpoint_ShouldRespondWith404NotFound_WhenSlugDoesNotExist()
    {
        BuildApp();

        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync($"{EndpointBaseUrl}/unknown-slug", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductBySlugEndpoint_ShouldRespondWith200Ok_AndProduct_WhenSlugIsCurrentAndProductPublished()
    {
        BuildApp();

        var arrangeClient = _app.RequestClient().UseAdminKey();

        var category = await arrangeClient.CreateCategoryAsync(
          name: "Bouquets",
          slug: $"bouquets-{_testId:N}"[..20],
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var slug = $"bouquet-{_testId:N}"[..20];
        var created = await arrangeClient.CreateProductAsync(
          categoryId: category.Id,
          name: "Spring Bouquet",
          slug: slug,
          seoTitle: "Spring Bouquet",
          fullDescription: "A selection of seasonal flowers",
          metaDescription: "Fresh spring bouquet",
          price: 45.50m,
          publishStatus: PublishStatus.Published,
          availability: ProductAvailabilityStatus.Available,
          isFeatured: true,
          cancellationToken: TestContext.Current.CancellationToken);

        await arrangeClient.UpdateProductImagesAsync(
          created.Id,
          created.RowVersion,
          images: [
            new UpdateProductImageItem
        {
          ImagePath = "/images/spring-bouquet-primary.jpg",
          IsPrimary = true,
          DisplayOrder = 1,
          AltText = "Spring bouquet"
        },
        new UpdateProductImageItem
        {
          ImagePath = "/images/spring-bouquet-secondary.jpg",
          IsPrimary = false,
          DisplayOrder = 2,
          AltText = "Bouquet detail"
        }
          ],
          cancellationToken: TestContext.Current.CancellationToken);

        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync($"{EndpointBaseUrl}/{slug}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GetProductBySlugResponse>(TestContext.Current.CancellationToken);
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
        nameof(persistedProduct.DisplayOrder),
        nameof(persistedProduct.PublishStatus),
        nameof(persistedProduct.Category),
        nameof(persistedProduct.Images),
        nameof(persistedProduct.CreatedAt),
        nameof(persistedProduct.UpdatedAt),
        nameof(persistedProduct.RowVersion),
        nameof(persistedProduct.DeletedAt)
          },
          equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        // Verify specific business logic and relationships
        payload.CategoryId.Should().Be(persistedProduct.CategoryId);
        payload.CategoryName.Should().Be(persistedProduct.Category.Name);

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
        }
    }

    [Fact]
    public async Task GetProductBySlugEndpoint_ShouldRespondWith301MovedPermanently_AndLocationHeader_WhenSlugIsStale()
    {
        BuildApp();

        var arrangeClient = _app.RequestClient().UseAdminKey();

        var category = await arrangeClient.CreateCategoryAsync(
          name: "Arrangements",
          slug: $"arrangements-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        var originalSlug = $"arrangement-{_testId:N}"[..20];
        var created = await arrangeClient.CreateProductAsync(
          categoryId: category.Id,
          name: "Sympathy Arrangement",
          slug: originalSlug,
          publishStatus: PublishStatus.Published,
          availability: ProductAvailabilityStatus.Available,
          cancellationToken: TestContext.Current.CancellationToken);

        var updatedSlug = $"arrangement-new-{_testId:N}"[..20];

        await arrangeClient.UpdateProductAsync(
          created.Id,
          created.RowVersion,
          slug: updatedSlug,
          cancellationToken: TestContext.Current.CancellationToken);

        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync($"{EndpointBaseUrl}/{originalSlug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Be($"{EndpointBaseUrl}/{updatedSlug}");
    }
}
