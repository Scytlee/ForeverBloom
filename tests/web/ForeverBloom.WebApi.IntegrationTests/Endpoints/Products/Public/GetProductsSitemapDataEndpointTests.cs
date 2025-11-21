using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Public.GetProductsSitemapData;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Public;

public sealed class GetProductsSitemapDataEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.GetProductsSitemapData;
    private const string EndpointUrl = "/api/v1/products/sitemap-data";

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

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.GetProductsSitemapData);
        ProductEndpointsModule.Names.GetProductsSitemapData.Should().Be("GetProductsSitemapData");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Public);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndSitemapData_WhenProductsExist()
    {
        var category = await Application.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var product1 = await Application.GivenProductAsync(
            categoryId: category.Id,
            publishStatus: PublishStatus.Published);

        var product2 = await Application.GivenProductAsync(
            categoryId: category.Id,
            publishStatus: PublishStatus.Published);

        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync(EndpointUrl, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<GetProductsSitemapDataResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Items.Should().Contain(item => item.Slug == product1.CurrentSlug);
        payload.Items.Should().Contain(item => item.Slug == product2.CurrentSlug);
        payload.Items.Should().AllSatisfy(item =>
        {
            item.Slug.Should().NotBeNullOrEmpty();
            item.UpdatedAt.Should().BeAfter(DateTimeOffset.MinValue);
        });
    }
}
