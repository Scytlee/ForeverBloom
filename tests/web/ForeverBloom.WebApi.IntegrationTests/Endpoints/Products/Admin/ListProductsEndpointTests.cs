using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Admin.ListProducts;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Admin;

public sealed class ListProductsEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.ListProducts;
    private const string EndpointUrl = "/api/v1/admin/products";

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

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.ListProducts);
        ProductEndpointsModule.Names.ListProducts.Should().Be("ListProducts");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndProductList_WhenRequestIsValid()
    {
        var category = await Application.GivenCategoryAsync();
        var product1 = await Application.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product Alpha {TestToken}");
        var product2 = await Application.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product Beta {TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.GetAsync(EndpointUrl, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ListProductsResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Items.Should().Contain(p => p.Id == product1.Id);
        payload.Items.Should().Contain(p => p.Id == product2.Id);
        payload.PageNumber.Should().BeGreaterThan(0);
        payload.PageSize.Should().BeGreaterThan(0);
        payload.TotalCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRequestToQueryValidationFails()
    {
        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.GetAsync($"{EndpointUrl}?PageNumber=0", CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
