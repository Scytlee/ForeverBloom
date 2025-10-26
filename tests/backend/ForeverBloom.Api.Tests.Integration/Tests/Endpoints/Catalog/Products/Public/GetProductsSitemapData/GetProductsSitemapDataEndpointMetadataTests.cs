using FluentAssertions;
using ForeverBloom.Api.Endpoints.Catalog.Products;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Public.GetProductsSitemapData;

public sealed class GetProductsSitemapDataEndpointMetadataTests : BackendAppTestClassBase
{
    private const string EndpointName = ProductEndpointsGroup.Names.GetProductsSitemapData;

    public GetProductsSitemapDataEndpointMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void GetProductsSitemapDataEndpoint_ShouldMapToCorrectRoute()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("GET");
        endpoint.ShouldHaveRoutePrefix("/api/v1/products");
    }

    [Fact]
    public void GetProductsSitemapDataEndpoint_ShouldHaveCorrectMetadata()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(ProductEndpointsGroup.Names.GetProductsSitemapData);
        ProductEndpointsGroup.Names.GetProductsSitemapData.Should().Be("GetProductsSitemapData");
        endpoint.ShouldHaveTag(ProductEndpointsGroup.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsGroup.Tags.Public);
    }
}
