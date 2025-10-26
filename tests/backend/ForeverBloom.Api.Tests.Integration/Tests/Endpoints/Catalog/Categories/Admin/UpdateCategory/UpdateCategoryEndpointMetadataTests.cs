using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.UpdateCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.UpdateCategory;

public sealed class UpdateCategoryEndpointMetadataTests : BackendAppTestClassBase
{
    private const string EndpointName = CategoryEndpointsGroup.Names.UpdateCategory;

    public UpdateCategoryEndpointMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void UpdateCategoryEndpoint_ShouldMapToCorrectRoute()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("PATCH");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/categories");
    }

    [Fact]
    public void UpdateCategoryEndpoint_ShouldHaveCorrectMetadata()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsGroup.Names.UpdateCategory);
        CategoryEndpointsGroup.Names.UpdateCategory.Should().Be("UpdateCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Admin);
    }

    [Fact]
    public void UpdateCategoryEndpoint_ShouldValidateRequest()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldValidateRequest<UpdateCategoryRequest>();
    }

    [Fact]
    public void UpdateCategoryEndpoint_ShouldUseUnitOfWork()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldUseUnitOfWork();
    }
}
