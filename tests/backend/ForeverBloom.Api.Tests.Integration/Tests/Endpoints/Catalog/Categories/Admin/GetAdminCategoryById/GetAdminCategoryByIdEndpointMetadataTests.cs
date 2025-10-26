using FluentAssertions;
using ForeverBloom.Api.Endpoints.Catalog.Categories;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.GetAdminCategoryById;

public sealed class GetAdminCategoryByIdEndpointMetadataTests : BackendAppTestClassBase
{
    private const string EndpointName = CategoryEndpointsGroup.Names.GetAdminCategoryById;

    public GetAdminCategoryByIdEndpointMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void GetAdminCategoryByIdEndpoint_ShouldMapToCorrectRoute()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("GET");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/categories");
    }

    [Fact]
    public void GetAdminCategoryByIdEndpoint_ShouldHaveCorrectMetadata()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsGroup.Names.GetAdminCategoryById);
        CategoryEndpointsGroup.Names.GetAdminCategoryById.Should().Be("GetAdminCategoryById");
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Admin);
    }
}
