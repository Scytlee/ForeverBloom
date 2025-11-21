using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Admin.CreateProduct;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Admin;

public sealed class CreateProductEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.CreateProduct;
    private const string EndpointUrl = "/api/v1/admin/products";

    [Fact]
    public void Endpoint_ShouldMapToCorrectRoute()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("POST");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/products");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.CreateProduct);
        ProductEndpointsModule.Names.CreateProduct.Should().Be("CreateProduct");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith201CreatedAndProductId_WhenValidRequestProvided()
    {
        var category = await Application.GivenCategoryAsync();
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateProductRequest(
            Name: $"Test Product {TestToken}",
            SeoTitle: null,
            FullDescription: null,
            MetaDescription: null,
            Slug: $"test-product-{TestToken}",
            CategoryId: category.Id,
            Price: null,
            IsFeatured: false,
            AvailabilityStatus: "coming_soon",
            Images: null);

        var httpResponse = await client.PostAsJsonAsync("admin/products", request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        httpResponse.Headers.Location.Should().NotBeNull();
        httpResponse.Headers.Location!.ToString().Should().Be($"/api/v1/admin/products/{request.Slug}");

        var payload = await httpResponse.Content.ReadFromJsonAsync<CreateProductResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRequestToCommandValidationFails()
    {
        var category = await Application.GivenCategoryAsync();
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateProductRequest(
            Name: $"Test Product {TestToken}",
            SeoTitle: null,
            FullDescription: null,
            MetaDescription: null,
            Slug: "INVALID_SLUG",
            CategoryId: category.Id,
            Price: null,
            IsFeatured: false,
            AvailabilityStatus: "coming_soon",
            Images: null);

        var httpResponse = await client.PostAsJsonAsync("admin/products", request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenSlugAlreadyInUse()
    {
        var category = await Application.GivenCategoryAsync();
        var slug = $"duplicate-slug-{TestToken}";

        await Application.GivenProductAsync(
            categoryId: category.Id,
            slug: slug);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateProductRequest(
            Name: $"Another Product {TestToken}",
            SeoTitle: null,
            FullDescription: null,
            MetaDescription: null,
            Slug: slug,
            CategoryId: category.Id,
            Price: null,
            IsFeatured: false,
            AvailabilityStatus: "coming_soon",
            Images: null);

        var httpResponse = await client.PostAsJsonAsync("admin/products", request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenCategoryNotFound()
    {
        const long nonExistentCategoryId = 9_999_999;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateProductRequest(
            Name: $"Test Product {TestToken}",
            SeoTitle: null,
            FullDescription: null,
            MetaDescription: null,
            Slug: $"test-product-{TestToken}",
            CategoryId: nonExistentCategoryId,
            Price: null,
            IsFeatured: false,
            AvailabilityStatus: "coming_soon",
            Images: null);

        var httpResponse = await client.PostAsJsonAsync("admin/products", request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenUseCaseReturnsUnhandledError()
    {
        var category = await Application.GivenCategoryAsync();
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateProductRequest(
            Name: $"Test Product {TestToken}",
            SeoTitle: null,
            FullDescription: null,
            MetaDescription: null,
            Slug: $"test-product-{TestToken}",
            CategoryId: category.Id,
            Price: null,
            IsFeatured: false,
            AvailabilityStatus: "coming_soon",
            Images: new[]
            {
                new CreateProductRequestImage(
                    Source: $"/images/{TestToken}-1.jpg",
                    AltText: $"Image 1 {TestToken}",
                    IsPrimary: true,
                    DisplayOrder: 1),
                new CreateProductRequestImage(
                    Source: $"/images/{TestToken}-2.jpg",
                    AltText: $"Image 2 {TestToken}",
                    IsPrimary: true,
                    DisplayOrder: 2)
            });

        var httpResponse = await client.PostAsJsonAsync("admin/products", request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
