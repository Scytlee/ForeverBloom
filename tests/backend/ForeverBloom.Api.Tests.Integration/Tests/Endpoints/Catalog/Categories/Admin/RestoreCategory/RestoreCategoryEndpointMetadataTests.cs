using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.RestoreCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.RestoreCategory;

public sealed class RestoreCategoryEndpointMetadataTests : BackendAppTestClassBase
{
    private const string EndpointName = CategoryEndpointsGroup.Names.RestoreCategory;

    public RestoreCategoryEndpointMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void RestoreCategoryEndpoint_ShouldMapToCorrectRoute()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("POST");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/categories");
    }

    [Fact]
    public void RestoreCategoryEndpoint_ShouldHaveCorrectMetadata()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsGroup.Names.RestoreCategory);
        CategoryEndpointsGroup.Names.RestoreCategory.Should().Be("RestoreCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Admin);
    }

    [Fact]
    public void RestoreCategoryEndpoint_ShouldValidateRequest()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldValidateRequest<RestoreCategoryRequest>();
    }

    [Fact]
    public void RestoreCategoryEndpoint_ShouldUseUnitOfWork()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldUseUnitOfWork();
    }
}
