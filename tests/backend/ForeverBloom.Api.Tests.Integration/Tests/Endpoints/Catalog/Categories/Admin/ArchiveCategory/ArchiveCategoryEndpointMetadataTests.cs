using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ArchiveCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Metadata;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.ArchiveCategory;

public sealed class ArchiveCategoryEndpointMetadataTests : BackendAppTestClassBase
{
    private const string EndpointName = CategoryEndpointsGroup.Names.ArchiveCategory;

    public ArchiveCategoryEndpointMetadataTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void ArchiveCategoryEndpoint_ShouldMapToCorrectRoute()
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
    public void ArchiveCategoryEndpoint_ShouldHaveCorrectMetadata()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsGroup.Names.ArchiveCategory);
        CategoryEndpointsGroup.Names.ArchiveCategory.Should().Be("ArchiveCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsGroup.Tags.Admin);
    }

    [Fact]
    public void ArchiveCategoryEndpoint_ShouldValidateRequest()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldValidateRequest<ArchiveCategoryRequest>();
    }

    [Fact]
    public void ArchiveCategoryEndpoint_ShouldUseUnitOfWork()
    {
        BuildApp();
        using var scope = _app.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldUseUnitOfWork();
    }
}
