using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Helpers.Validation;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.CreateProduct;

public sealed class CreateProductEndpointTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/admin/products";

    public CreateProductEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task CreateProductEndpoint_ShouldRespondWith400BadRequest_WhenSlugIsNotAvailable()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();
        var category = await client.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var slug = $"prod-{_testId:N}"[..20];

        await client.CreateProductAsync(
          categoryId: category.Id,
          name: "First Product",
          slug: slug,
          cancellationToken: TestContext.Current.CancellationToken);

        var second = new CreateProductRequest
        {
            Name = "Second Product",
            Slug = slug,
            CategoryId = category.Id
        };

        var secondResponse = await client.PostAsJsonAsync(EndpointUrl, second, TestContext.Current.CancellationToken);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errors = await secondResponse.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(CreateProductRequest.Slug), ProductValidation.ErrorCodes.SlugIsNotAvailable);
    }

    [Fact]
    public async Task CreateProductEndpoint_ShouldRespondWith201Created_AndProduct_WhenCreationIsSuccessful()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();
        var category = await client.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);
        var slug = $"prod-{_testId:N}"[..20];
        var request = new CreateProductRequest
        {
            Name = "Complete Test Product",
            Slug = slug,
            SeoTitle = "Complete Test Product SEO",
            FullDescription = "This is a complete description of the test product with all fields populated",
            MetaDescription = "Complete test product meta description",
            Price = 199.99m,
            DisplayOrder = 10,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.Available,
            CategoryId = category.Id
        };

        var response = await client.PostAsJsonAsync(EndpointUrl, request, TestContext.Current.CancellationToken);

        // Verify response
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseContent = await response.Content.ReadFromJsonAsync<CreateProductResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().EndWith($"{EndpointUrl}/{responseContent.Id}");

        // Verify database state
        var persistedProduct = await DbContext.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == responseContent.Id, TestContext.Current.CancellationToken);
        persistedProduct.Should().NotBeNull();
        persistedProduct.CreatedAt.Should().BeAfter(DateTimeOffset.MinValue);
        persistedProduct.UpdatedAt.Should().BeAfter(DateTimeOffset.MinValue);
        persistedProduct.CreatedAt.Should().BeExactly(persistedProduct.UpdatedAt);
        persistedProduct.DeletedAt.Should().BeNull();
        persistedProduct.RowVersion.Should().BeGreaterThan(0);
        AssertionHelpers.AssertAllPropertiesAreMapped(
          sourceObject: request,
          destinationObject: persistedProduct,
          overridesMap: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
          {
        { nameof(request.Slug), nameof(persistedProduct.CurrentSlug) }
          });

        // Verify response content
        AssertionHelpers.AssertAllPropertiesAreMapped(
          sourceObject: persistedProduct,
          destinationObject: responseContent,
          overridesMap: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
          {
        { nameof(persistedProduct.CurrentSlug), nameof(responseContent.Slug) }
          },
          sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
          {
        nameof(persistedProduct.Category),
        nameof(persistedProduct.Images)
          },
          equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));
        responseContent.CategoryId.Should().Be(request.CategoryId);

        // Verify slug registry entry
        var slugEntry = await DbContext.SlugRegistry.AsNoTracking().SingleOrDefaultAsync(s => s.Slug == request.Slug, TestContext.Current.CancellationToken);
        slugEntry.Should().NotBeNull();
        slugEntry.EntityType.Should().Be(EntityType.Product);
        slugEntry.EntityId.Should().Be(persistedProduct.Id);
        slugEntry.IsActive.Should().BeTrue();
    }
}
