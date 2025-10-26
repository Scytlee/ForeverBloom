using FluentAssertions;
using ForeverBloom.Api.Endpoints.Catalog.Products;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Public.ListProducts;

public sealed class ListProductsEndpointMetadataTests : BackendAppTestClassBase
{
    private const string EndpointName = ProductEndpointsGroup.Names.ListProducts;

    public ListProductsEndpointMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void ListProductsEndpoint_ShouldMapToCorrectRoute()
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
    public void ListProductsEndpoint_ShouldHaveCorrectMetadata()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(ProductEndpointsGroup.Names.ListProducts);
        ProductEndpointsGroup.Names.ListProducts.Should().Be("ListProducts");
        endpoint.ShouldHaveTag(ProductEndpointsGroup.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsGroup.Tags.Public);
    }
}
