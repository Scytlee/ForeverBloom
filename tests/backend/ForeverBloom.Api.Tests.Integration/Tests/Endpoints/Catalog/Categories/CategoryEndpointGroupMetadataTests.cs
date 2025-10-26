using FluentAssertions;
using ForeverBloom.Api.Authentication;
using ForeverBloom.Api.Endpoints.Catalog.Categories;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories;

public sealed class CategoryEndpointGroupMetadataTests : BackendAppTestClassBase
{
    public CategoryEndpointGroupMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void CategoryAdminEndpoints_ShouldHaveAdminAccessAuthorizationPolicy()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var adminEndpoints = EndpointMetadataTestHelper.GetEndpointsByTags(dataSource, CategoryEndpointsGroup.Tags.Categories, CategoryEndpointsGroup.Tags.Admin);

        adminEndpoints.Should().NotBeEmpty();
        foreach (var endpoint in adminEndpoints)
        {
            endpoint.ShouldHaveAuthorizationPolicy(ApiKeyAuthenticationDefaults.AdminAccessPolicyName);
        }
    }

    [Fact]
    public void CategoryPublicEndpoints_ShouldHaveFrontendAccessAuthorizationPolicy()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var publicEndpoints = EndpointMetadataTestHelper.GetEndpointsByTags(dataSource, CategoryEndpointsGroup.Tags.Categories, CategoryEndpointsGroup.Tags.Public);

        publicEndpoints.Should().NotBeEmpty();
        foreach (var endpoint in publicEndpoints)
        {
            endpoint.ShouldHaveAuthorizationPolicy(ApiKeyAuthenticationDefaults.FrontendAccessPolicyName);
        }
    }

    [Fact]
    public void CategoryGroupTags_ShouldBeCorrectStrings()
    {
        CategoryEndpointsGroup.Tags.Categories.Should().Be("Categories");
        CategoryEndpointsGroup.Tags.Public.Should().Be("Public");
        CategoryEndpointsGroup.Tags.Admin.Should().Be("Admin");
    }
}
