using FluentAssertions;
using ForeverBloom.Api.Authentication;
using ForeverBloom.Api.Endpoints.Catalog.Products;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products;

public sealed class ProductEndpointGroupMetadataTests : BackendAppTestClassBase
{
    public ProductEndpointGroupMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void ProductAdminEndpoints_ShouldHaveAdminAccessAuthorizationPolicy()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var adminEndpoints = EndpointMetadataTestHelper.GetEndpointsByTags(dataSource, ProductEndpointsGroup.Tags.Products, ProductEndpointsGroup.Tags.Admin);

        adminEndpoints.Should().NotBeEmpty();
        foreach (var endpoint in adminEndpoints)
        {
            endpoint.ShouldHaveAuthorizationPolicy(ApiKeyAuthenticationDefaults.AdminAccessPolicyName);
        }
    }

    [Fact]
    public void ProductPublicEndpoints_ShouldHaveFrontendAccessAuthorizationPolicy()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var publicEndpoints = EndpointMetadataTestHelper.GetEndpointsByTags(dataSource, ProductEndpointsGroup.Tags.Products, ProductEndpointsGroup.Tags.Public);

        publicEndpoints.Should().NotBeEmpty();
        foreach (var endpoint in publicEndpoints)
        {
            endpoint.ShouldHaveAuthorizationPolicy(ApiKeyAuthenticationDefaults.FrontendAccessPolicyName);
        }
    }

    [Fact]
    public void ProductGroupTags_ShouldBeCorrectStrings()
    {
        ProductEndpointsGroup.Tags.Products.Should().Be("Products");
        ProductEndpointsGroup.Tags.Public.Should().Be("Public");
        ProductEndpointsGroup.Tags.Admin.Should().Be("Admin");
    }
}
