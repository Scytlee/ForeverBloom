using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Admin.CreateCategory;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Admin;

public sealed class CreateCategoryEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.CreateCategory;
    private const string EndpointUrl = "/api/v1/admin/categories";

    [Fact]
    public void Endpoint_ShouldMapToCorrectRoute()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("POST");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/categories");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.CreateCategory);
        CategoryEndpointsModule.Names.CreateCategory.Should().Be("CreateCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith201CreatedAndCategoryId_WhenValidRequestProvided()
    {
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateCategoryRequest(
            Name: $"Test Category {TestToken}",
            Description: $"Test description {TestToken}",
            Slug: $"test-category-{TestToken}",
            ImagePath: $"/images/{TestToken}.jpg",
            ImageAltText: $"Test image {TestToken}",
            ParentCategoryId: null,
            DisplayOrder: 1);

        var httpResponse = await client.PostAsJsonAsync(EndpointUrl, request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        httpResponse.Headers.Location.Should().NotBeNull();
        httpResponse.Headers.Location!.ToString().Should().Be($"{EndpointUrl}/{request.Slug}");

        var payload = await httpResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRequestToCommandValidationFails()
    {
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateCategoryRequest(
            Name: $"Test Category {TestToken}",
            Description: null,
            Slug: "INVALID_SLUG",
            ImagePath: null,
            ImageAltText: null,
            ParentCategoryId: null,
            DisplayOrder: 0);

        var httpResponse = await client.PostAsJsonAsync(EndpointUrl, request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenParentCategoryNotFound()
    {
        const long nonExistentParentId = 9_999_999;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateCategoryRequest(
            Name: $"Test Category {TestToken}",
            Description: null,
            Slug: $"test-category-{TestToken}",
            ImagePath: null,
            ImageAltText: null,
            ParentCategoryId: nonExistentParentId,
            DisplayOrder: 0);

        var httpResponse = await client.PostAsJsonAsync(EndpointUrl, request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenSlugAlreadyInUse()
    {
        var slug = $"duplicate-slug-{TestToken}";

        await Application.GivenCategoryAsync(
            name: $"Existing Category {TestToken}",
            slug: slug);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateCategoryRequest(
            Name: $"Another Category {TestToken}",
            Description: null,
            Slug: slug,
            ImagePath: null,
            ImageAltText: null,
            ParentCategoryId: null,
            DisplayOrder: 0);

        var httpResponse = await client.PostAsJsonAsync(EndpointUrl, request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenNameNotUniqueWithinParent()
    {
        var duplicateName = $"Duplicate Name {TestToken}";

        await Application.GivenCategoryAsync(
            name: duplicateName,
            slug: $"existing-slug-{TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new CreateCategoryRequest(
            Name: duplicateName,
            Description: null,
            Slug: $"different-slug-{TestToken}",
            ImagePath: null,
            ImageAltText: null,
            ParentCategoryId: null,
            DisplayOrder: 0);

        var httpResponse = await client.PostAsJsonAsync(EndpointUrl, request, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
