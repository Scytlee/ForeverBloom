using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.WebApi.Authentication;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products;

public sealed class ProductEndpointsModuleMetadataTests : WebApiIntegrationTestBase
{
    [Fact]
    public void AdminEndpoints_ShouldHaveAdminAccessAuthorizationPolicy()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var adminEndpoints = EndpointMetadataTestHelper.GetEndpointsByTags(
            dataSource,
            ProductEndpointsModule.Tags.Products,
            ProductEndpointsModule.Tags.Admin);

        adminEndpoints.Should().NotBeEmpty();
        foreach (var endpoint in adminEndpoints)
        {
            endpoint.ShouldHaveAuthorizationPolicy(ApiKeyAuthenticationDefaults.AdminAccessPolicyName);
        }
    }

    [Fact]
    public void PublicEndpoints_ShouldHaveFrontendAccessAuthorizationPolicy()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var publicEndpoints = EndpointMetadataTestHelper.GetEndpointsByTags(
            dataSource,
            ProductEndpointsModule.Tags.Products,
            ProductEndpointsModule.Tags.Public);

        publicEndpoints.Should().NotBeEmpty();
        foreach (var endpoint in publicEndpoints)
        {
            endpoint.ShouldHaveAuthorizationPolicy(ApiKeyAuthenticationDefaults.FrontendAccessPolicyName);
        }
    }

    [Fact]
    public void AllEndpoints_ShouldEnrichProblemDetails()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoints = EndpointMetadataTestHelper.GetEndpointsByTags(
            dataSource,
            ProductEndpointsModule.Tags.Products);

        endpoints.Should().NotBeEmpty();
        foreach (var endpoint in endpoints)
        {
            endpoint.ShouldEnrichProblemDetails();
        }
    }

    [Fact]
    public void Tags_ShouldBeCorrectStrings()
    {
        ProductEndpointsModule.Tags.Products.Should().Be("Products");
        ProductEndpointsModule.Tags.Public.Should().Be("Public");
        ProductEndpointsModule.Tags.Admin.Should().Be("Admin");
    }
}
