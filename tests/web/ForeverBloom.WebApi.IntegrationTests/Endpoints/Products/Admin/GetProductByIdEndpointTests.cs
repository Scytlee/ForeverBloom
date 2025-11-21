using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Admin.GetProductById;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Admin;

public sealed class GetProductByIdEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.GetProductById;
    private static string EndpointUrl(long productId) => $"/api/v1/admin/products/{productId}";

    [Fact]
    public void Endpoint_ShouldMapToCorrectRoute()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("GET");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/products");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.GetProductById);
        ProductEndpointsModule.Names.GetProductById.Should().Be("GetProductById");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndProductDetails_WhenProductExists()
    {
        var category = await Application.GivenCategoryAsync();

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

        var creationTimestamp = Fixture.TimeProvider.CurrentTime;
        var product = await Application.GivenProductAsync(
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
            creationTimestamp: creationTimestamp);

        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.GetAsync(EndpointUrl(product.Id), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<GetProductByIdResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Id.Should().Be(product.Id);
        payload.Name.Should().Be(name);
        payload.Slug.Should().Be(slug);
        payload.SeoTitle.Should().Be(seoTitle);
        payload.FullDescription.Should().Be(fullDescription);
        payload.MetaDescription.Should().Be(metaDescription);
        payload.Price.Should().Be(price);
        payload.CategoryId.Should().Be(category.Id);
        payload.IsFeatured.Should().BeTrue();
        payload.PublishStatus.Should().Be("draft");
        payload.AvailabilityStatus.Should().Be("available");
        payload.Images.Should().HaveCount(2);
        payload.CreatedAt.Should().Be(creationTimestamp);
        payload.UpdatedAt.Should().Be(creationTimestamp);
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().Be(product.RowVersion);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenProductIdIsInvalid()
    {
        const long invalidProductId = 0;
        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.GetAsync(EndpointUrl(invalidProductId), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        const long nonExistentProductId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.GetAsync(EndpointUrl(nonExistentProductId), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
