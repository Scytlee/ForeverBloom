using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Public.BrowseCatalogProducts;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Public;

public sealed class BrowseCatalogProductsEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.BrowseCatalogProducts;
    private static string BaseEndpointUrl => "/api/v1/products";

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

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.BrowseCatalogProducts);
        ProductEndpointsModule.Names.BrowseCatalogProducts.Should().Be("BrowseCatalogProducts");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Public);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndProducts_WhenRequestIsValid()
    {
        var category = await Application.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        await Application.GivenProductAsync(
            categoryId: category.Id,
            name: $"Test Product {TestToken}",
            price: 99.99m,
            publishStatus: PublishStatus.Published);

        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync(BaseEndpointUrl, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<BrowseCatalogProductsResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Items.Should().OnlyContain(p => p.CategoryId == category.Id);
        payload.PageNumber.Should().BeGreaterThan(0);
        payload.PageSize.Should().BeGreaterThan(0);
        payload.TotalCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRequestValidationFails()
    {
        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync($"{BaseEndpointUrl}?PageNumber=0", CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
