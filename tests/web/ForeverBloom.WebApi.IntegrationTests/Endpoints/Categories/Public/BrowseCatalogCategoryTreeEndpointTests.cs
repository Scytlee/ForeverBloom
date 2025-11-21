using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Public.BrowseCatalogCategoryTree;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Public;

public sealed class BrowseCatalogCategoryTreeEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.BrowseCatalogCategoryTree;
    private static string BaseEndpointUrl => "/api/v1/categories/tree";

    [Fact]
    public void Endpoint_ShouldMapToCorrectRoute()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("GET");
        endpoint.ShouldHaveRoutePrefix("/api/v1/categories");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.BrowseCatalogCategoryTree);
        CategoryEndpointsModule.Names.BrowseCatalogCategoryTree.Should().Be("BrowseCatalogCategoryTree");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Public);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndCategoryTree_WhenRequestIsValid()
    {
        var rootCategory = await Application.GivenCategoryAsync(
            name: $"Root Category {TestToken}",
            slug: $"root-{TestToken}",
            publishStatus: PublishStatus.Published);

        await Application.GivenCategoryAsync(
            name: $"Child Category {TestToken}",
            slug: $"child-{TestToken}",
            parentCategoryId: rootCategory.Id,
            publishStatus: PublishStatus.Published);

        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync(BaseEndpointUrl, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<BrowseCatalogCategoryTreeResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Categories.Should().NotBeEmpty();
        payload.Categories.Should().Contain(c => c.Id == rootCategory.Id);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRootCategoryIdIsInvalid()
    {
        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync($"{BaseEndpointUrl}?RootCategoryId=0", CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
