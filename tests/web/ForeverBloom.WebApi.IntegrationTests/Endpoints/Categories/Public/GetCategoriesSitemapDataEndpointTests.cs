using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Public.GetCategoriesSitemapData;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Public;

public sealed class GetCategoriesSitemapDataEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.GetCategoriesSitemapData;
    private const string EndpointUrl = "/api/v1/categories/sitemap-data";

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

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.GetCategoriesSitemapData);
        CategoryEndpointsModule.Names.GetCategoriesSitemapData.Should().Be("GetCategoriesSitemapData");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Public);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndSitemapData_WhenCategoriesExist()
    {
        var category1 = await Application.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var category2 = await Application.GivenCategoryAsync(
            publishStatus: PublishStatus.Published);

        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync(EndpointUrl, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<GetCategoriesSitemapDataResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Items.Should().HaveCount(2);
        payload.Items.Should().Contain(item => item.Slug == category1.CurrentSlug);
        payload.Items.Should().Contain(item => item.Slug == category2.CurrentSlug);
        payload.Items.Should().AllSatisfy(item =>
        {
            item.Slug.Should().NotBeNullOrEmpty();
            item.UpdatedAt.Should().BeAfter(DateTimeOffset.MinValue);
        });
    }
}
