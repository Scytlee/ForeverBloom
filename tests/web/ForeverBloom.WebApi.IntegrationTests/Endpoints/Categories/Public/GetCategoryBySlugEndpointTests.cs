using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Public.GetCategoryBySlug;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Public;

public sealed class GetCategoryBySlugEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.GetCategoryBySlug;

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

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.GetCategoryBySlug);
        CategoryEndpointsModule.Names.GetCategoryBySlug.Should().Be("GetCategoryBySlug");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Public);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndCategoryDetails_WhenCategoryExists()
    {
        var name = $"Test category {TestToken}";
        var slug = $"category-{TestToken}";
        var description = $"Test category description {TestToken}";

        await Application.GivenCategoryAsync(
            name: name,
            slug: slug,
            description: description,
            publishStatus: PublishStatus.Published);
        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync($"categories/{slug}", CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<GetCategoryBySlugResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Name.Should().Be(name);
        payload.Slug.Should().Be(slug);
        payload.Description.Should().Be(description);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith301MovedPermanentlyAndLocationHeader_WhenHistoricalSlugProvided()
    {
        var oldSlug = $"old-slug-{TestToken}";
        var newSlug = $"new-slug-{TestToken}";

        await Application.GivenCategoryAsync(
            slug: newSlug,
            publishStatus: PublishStatus.Published,
            slugHistory: [oldSlug]);
        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync($"categories/{oldSlug}", CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        httpResponse.Headers.Location.Should().NotBeNull();
        httpResponse.Headers.Location!.ToString().Should().Be($"/api/v1/categories/{newSlug}");
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenCategoryNotFound()
    {
        var nonExistentSlug = $"non-existent-{TestToken}";
        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync($"categories/{nonExistentSlug}", CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenSlugFormatIsInvalid()
    {
        var invalidSlug = $"OBVIOUSLY.INVALID_SLUG-{TestToken}";
        var client = CreateClient(WebApiKeyScope.Frontend);

        var httpResponse = await client.GetAsync($"categories/{invalidSlug}", CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
