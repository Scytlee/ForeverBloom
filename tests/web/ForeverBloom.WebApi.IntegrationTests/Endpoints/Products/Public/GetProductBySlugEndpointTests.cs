using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Public.GetProductBySlug;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Public;

public sealed class GetProductBySlugEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.GetProductBySlug;
    private static string EndpointUrl(string slug) => $"/api/v1/products/{slug}";

    [Fact]
    public void Endpoint_ShouldMapToCorrectRoute()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("GET");
        endpoint.ShouldHaveRoutePrefix("/api/v1/products");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.GetProductBySlug);
        ProductEndpointsModule.Names.GetProductBySlug.Should().Be("GetProductBySlug");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Public);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndProductDetails_WhenProductExists()
    {
        var category = await Application.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var name = $"Premium Bouquet {TestToken}";
        var slug = $"premium-bouquet-{TestToken}";
        var seoTitle = $"Premium Bouquet SEO {TestToken}";
        var fullDescription = $"<p>Beautiful arrangement {TestToken}</p>";
        var metaDescription = $"Premium floral bouquet {TestToken}";
        const decimal price = 149.99m;

        var images = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-primary.jpg", $"Primary {TestToken}", true, 1),
            new CreateProductCommandImage($"/images/{TestToken}-secondary.jpg", $"Secondary {TestToken}", false, 2)
        };

        await Application.GivenProductAsync(
            categoryId: category.Id,
            name: name,
            slug: slug,
            seoTitle: seoTitle,
            fullDescription: fullDescription,
            metaDescription: metaDescription,
            price: price,
            isFeatured: true,
            availabilityStatus: ProductAvailabilityStatus.Available,
            images: images,
            publishStatus: PublishStatus.Published);

        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync(EndpointUrl(slug), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<GetProductBySlugResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Name.Should().Be(name);
        payload.Slug.Should().Be(slug);
        payload.SeoTitle.Should().Be(seoTitle);
        payload.FullDescription.Should().Be(fullDescription);
        payload.MetaDescription.Should().Be(metaDescription);
        payload.Price.Should().Be(price);
        payload.CategoryId.Should().Be(category.Id);
        payload.IsFeatured.Should().BeTrue();
        payload.AvailabilityStatus.Should().Be("available");
        payload.Images.Should().HaveCount(2);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenSlugFormatIsInvalid()
    {
        var invalidSlug = $"INVALID.SLUG_WITH-{TestToken}";
        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync(EndpointUrl(invalidSlug), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenSlugNotFound()
    {
        var nonExistentSlug = $"non-existent-{TestToken}";
        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync(EndpointUrl(nonExistentSlug), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith301MovedPermanentlyAndLocationHeader_WhenHistoricalSlugProvided()
    {
        var category = await Application.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var oldSlug = $"old-product-slug-{TestToken}";
        var newSlug = $"new-product-slug-{TestToken}";

        await Application.GivenProductAsync(
            categoryId: category.Id,
            slug: newSlug,
            publishStatus: PublishStatus.Published,
            slugHistory: [oldSlug]);

        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync(EndpointUrl(oldSlug), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        httpResponse.Headers.Location.Should().NotBeNull();
        httpResponse.Headers.Location!.ToString().Should().Be(EndpointUrl(newSlug));
    }
}
