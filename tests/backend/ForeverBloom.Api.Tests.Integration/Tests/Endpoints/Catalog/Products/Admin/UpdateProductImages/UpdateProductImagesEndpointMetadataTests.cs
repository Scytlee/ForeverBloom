using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProductImages;
using ForeverBloom.Api.Endpoints.Catalog.Products;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.UpdateProductImages;

public sealed class UpdateProductImagesEndpointMetadataTests : BackendAppTestClassBase
{
    private const string EndpointName = ProductEndpointsGroup.Names.UpdateProductImages;

    public UpdateProductImagesEndpointMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void UpdateProductImagesEndpoint_ShouldMapToCorrectRoute()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("PUT");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/products");
    }

    [Fact]
    public void UpdateProductImagesEndpoint_ShouldHaveCorrectMetadata()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(ProductEndpointsGroup.Names.UpdateProductImages);
        ProductEndpointsGroup.Names.UpdateProductImages.Should().Be("UpdateProductImages");
        endpoint.ShouldHaveTag(ProductEndpointsGroup.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsGroup.Tags.Admin);
    }

    [Fact]
    public void UpdateProductImagesEndpoint_ShouldValidateRequest()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldValidateRequest<UpdateProductImagesRequest>();
    }

    [Fact]
    public void UpdateProductImagesEndpoint_ShouldUseUnitOfWork()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldUseUnitOfWork();
    }
}
