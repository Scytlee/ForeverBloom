using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.WebApi.Authentication;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories;

public sealed class CategoryEndpointsModuleMetadataTests : WebApiIntegrationTestBase
{
    [Fact]
    public void AdminEndpoints_ShouldHaveAdminAccessAuthorizationPolicy()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var adminEndpoints = EndpointMetadataTestHelper.GetEndpointsByTags(
            dataSource,
            CategoryEndpointsModule.Tags.Categories,
            CategoryEndpointsModule.Tags.Admin);

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
            CategoryEndpointsModule.Tags.Categories,
            CategoryEndpointsModule.Tags.Public);

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
            CategoryEndpointsModule.Tags.Categories);

        endpoints.Should().NotBeEmpty();
        foreach (var endpoint in endpoints)
        {
            endpoint.ShouldEnrichProblemDetails();
        }
    }

    [Fact]
    public void Tags_ShouldBeCorrectStrings()
    {
        CategoryEndpointsModule.Tags.Categories.Should().Be("Categories");
        CategoryEndpointsModule.Tags.Public.Should().Be("Public");
        CategoryEndpointsModule.Tags.Admin.Should().Be("Admin");
    }
}
