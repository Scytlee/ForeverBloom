using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.DeleteCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.DeleteCategory;

public sealed class DeleteCategoryEndpointMetadataTests : BackendAppTestClassBase
{
    private const string EndpointName = CategoryEndpointsGroup.Names.DeleteCategory;

    public DeleteCategoryEndpointMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void DeleteCategoryEndpoint_ShouldMapToCorrectRoute()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("DELETE");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/categories");
    }

    [Fact]
    public void DeleteCategoryEndpoint_ShouldHaveCorrectMetadata()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsGroup.Names.DeleteCategory);
        CategoryEndpointsGroup.Names.DeleteCategory.Should().Be("DeleteCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Admin);
    }

    [Fact]
    public void DeleteCategoryEndpoint_ShouldValidateRequest()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldValidateRequest<DeleteCategoryRequest>();
    }

    [Fact]
    public void DeleteCategoryEndpoint_ShouldUseUnitOfWork()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldUseUnitOfWork();
    }
}
