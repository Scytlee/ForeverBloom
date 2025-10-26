using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProduct;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Helpers.Validation;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.UpdateProduct;

public sealed class UpdateProductEndpointTests : BackendAppTestClassBase
{
    private static string EndpointUrl(int productId) => $"/api/v1/admin/products/{productId}";

    public UpdateProductEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    private static UpdateProductRequest BuildUpdateProductRequest(
      Optional<string>? name = null,
      Optional<string?>? seoTitle = null,
      Optional<string?>? fullDescription = null,
      Optional<string?>? metaDescription = null,
      Optional<string>? slug = null,
      Optional<decimal?>? price = null,
      Optional<int>? categoryId = null,
      Optional<int>? displayOrder = null,
      Optional<bool>? isFeatured = null,
      Optional<PublishStatus>? publishStatus = null,
      Optional<ProductAvailabilityStatus>? availability = null,
      uint? rowVersion = null)
    {
        return new UpdateProductRequest
        {
            Name = name ?? Optional<string>.Unset,
            SeoTitle = seoTitle ?? Optional<string?>.Unset,
            FullDescription = fullDescription ?? Optional<string?>.Unset,
            MetaDescription = metaDescription ?? Optional<string?>.Unset,
            Slug = slug ?? Optional<string>.Unset,
            Price = price ?? Optional<decimal?>.Unset,
            CategoryId = categoryId ?? Optional<int>.Unset,
            DisplayOrder = displayOrder ?? Optional<int>.Unset,
            IsFeatured = isFeatured ?? Optional<bool>.Unset,
            PublishStatus = publishStatus ?? Optional<PublishStatus>.Unset,
            Availability = availability ?? Optional<ProductAvailabilityStatus>.Unset,
            RowVersion = rowVersion ?? 1
        };
    }

    [Fact]
    public async Task UpdateProductEndpoint_ShouldRespondWith200Ok_WhenUpdateIsSuccessful()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Create another category for testing category change
        var newCategory = await client.CreateCategoryAsync(
          name: $"NewCategory-{_testId:N}"[..20],
          slug: $"newcat-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Create a product
        var createdProduct = await client.CreateProductAsync(
          categoryId: createdCategory.Id,
          name: $"Original-{_testId:N}"[..20],
          slug: $"orig-{_testId:N}"[..20],
          price: 99.99m,
          displayOrder: 5,
          isFeatured: false,
          publishStatus: PublishStatus.Draft,
          availability: ProductAvailabilityStatus.Available,
          cancellationToken: TestContext.Current.CancellationToken);

        // Act: Update the product with multiple field changes including slug and category
        var updateRequest = BuildUpdateProductRequest(
          name: Optional<string>.FromValue($"Updated-{_testId:N}"[..20]),
          seoTitle: Optional<string?>.FromValue("Updated SEO Title"),
          fullDescription: Optional<string?>.FromValue("Updated full description"),
          metaDescription: Optional<string?>.FromValue("Updated meta description"),
          slug: Optional<string>.FromValue($"updated-{_testId:N}"[..20]),
          price: Optional<decimal?>.FromValue(199.99m),
          categoryId: Optional<int>.FromValue(newCategory.Id),
          displayOrder: Optional<int>.FromValue(10),
          isFeatured: Optional<bool>.FromValue(true),
          publishStatus: Optional<PublishStatus>.FromValue(PublishStatus.Published),
          availability: Optional<ProductAvailabilityStatus>.FromValue(ProductAvailabilityStatus.Discontinued),
          rowVersion: createdProduct.RowVersion);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(createdProduct.Id));
        httpRequest.Content = JsonContent.Create(updateRequest, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // Assert response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<UpdateProductResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        // Assert database state
        var persistedProduct = await DbContext.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.Should().NotBeNull();
        persistedProduct.UpdatedAt.Should().BeAfter(persistedProduct.CreatedAt);
        persistedProduct.RowVersion.Should().BeGreaterThan(createdProduct.RowVersion);

        // Verify specific field updates
        persistedProduct.Name.Should().Be(updateRequest.Name.Value);
        persistedProduct.SeoTitle.Should().Be(updateRequest.SeoTitle.Value);
        persistedProduct.FullDescription.Should().Be(updateRequest.FullDescription.Value);
        persistedProduct.MetaDescription.Should().Be(updateRequest.MetaDescription.Value);
        persistedProduct.CurrentSlug.Should().Be(updateRequest.Slug.Value);
        persistedProduct.Price.Should().Be(updateRequest.Price.Value);
        persistedProduct.CategoryId.Should().Be(updateRequest.CategoryId.Value);
        persistedProduct.DisplayOrder.Should().Be(updateRequest.DisplayOrder.Value);
        persistedProduct.IsFeatured.Should().Be(updateRequest.IsFeatured.Value);
        persistedProduct.PublishStatus.Should().Be(updateRequest.PublishStatus.Value);
        persistedProduct.Availability.Should().Be(updateRequest.Availability.Value);

        // Verify response content maps correctly to persisted entity
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

        // Verify CategoryName and CategorySlug reflect new category in response
        responseContent.CategoryName.Should().Be(newCategory.Name);
        responseContent.CategorySlug.Should().Be(newCategory.Slug);

        // Verify slug registry updated when slug changes
        var newSlugEntry = await DbContext.SlugRegistry.AsNoTracking().SingleAsync(s => s.Slug == updateRequest.Slug.Value, TestContext.Current.CancellationToken);
        newSlugEntry.EntityType.Should().Be(EntityType.Product);
        newSlugEntry.EntityId.Should().Be(createdProduct.Id);
        newSlugEntry.IsActive.Should().BeTrue();

        // Verify old slug registry entry is deactivated
        var oldSlugEntry = await DbContext.SlugRegistry.AsNoTracking().SingleAsync(s => s.Slug == createdProduct.Slug, TestContext.Current.CancellationToken);
        oldSlugEntry.Should().NotBeNull();
        oldSlugEntry.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProductEndpoint_ShouldRespondWith400BadRequest_WhenCategoryDoesNotExist()
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

        // Act: Try to update product with non-existent CategoryId (DB roundtrip)
        var updateRequest = BuildUpdateProductRequest(
          categoryId: Optional<int>.FromValue(999999),
          rowVersion: createdProduct.RowVersion);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(createdProduct.Id));
        httpRequest.Content = JsonContent.Create(updateRequest, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(UpdateProductRequest.CategoryId), ProductValidation.ErrorCodes.CategoryNotFound);

        // Verify database unchanged
        var persistedProduct = await DbContext.Products.AsNoTracking().SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.CategoryId.Should().Be(createdCategory.Id); // Should still be original category
        persistedProduct.RowVersion.Should().Be(createdProduct.RowVersion); // Should not be incremented
    }

    [Fact]
    public async Task UpdateProductEndpoint_ShouldRespondWith404NotFound_WhenProductNotFound()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Act: Try to update non-existent product
        var updateRequest = BuildUpdateProductRequest(
          name: Optional<string>.FromValue($"Updated-{_testId:N}"[..20]),
          rowVersion: 1);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(999999));
        httpRequest.Content = JsonContent.Create(updateRequest, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProductEndpoint_ShouldRespondWith409Conflict_WhenConcurrencyConflictOccurs()
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

        // Act: Try to update with stale row version (EF concurrency catch path)
        var updateRequest = BuildUpdateProductRequest(
          name: Optional<string>.FromValue($"My-Update-{_testId:N}"[..20]),
          rowVersion: createdProduct.RowVersion); // This is now stale

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(createdProduct.Id));
        httpRequest.Content = JsonContent.Create(updateRequest, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Verify database unchanged by update attempt
        var persistedProduct = await DbContext.Products.AsNoTracking().SingleAsync(p => p.Id == createdProduct.Id, TestContext.Current.CancellationToken);
        persistedProduct.Name.Should().Be("Concurrently Modified");
        persistedProduct.Name.Should().NotBe(updateRequest.Name.Value);
    }
}
