using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Admin.ReparentCategory;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Admin;

public sealed class ReparentCategoryEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.ReparentCategory;
    private static string EndpointUrl(long categoryId) => $"/api/v1/admin/categories/{categoryId}:reparent";

    private static JsonSerializerOptions GetJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());
        return options;
    }

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

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.ReparentCategory);
        CategoryEndpointsModule.Names.ReparentCategory.Should().Be("ReparentCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndReparentedCategory_WhenValidRequest()
    {
        var category = await Application.GivenCategoryAsync(
            name: $"Category-{TestToken}",
            slug: $"category-{TestToken}");

        var newParent = await Application.GivenCategoryAsync(
            name: $"NewParent-{TestToken}",
            slug: $"new-parent-{TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReparentCategoryRequest(
            NewParentCategoryId: newParent.Id,
            RowVersion: category.RowVersion);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUrl(category.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ReparentCategoryResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Path.Should().Be($"{newParent.Path.Value}.{category.CurrentSlug.Value}");
        payload.ParentCategoryId.Should().Be(newParent.Id);
        payload.RowVersion.Should().BeGreaterThan(category.RowVersion);
        payload.UpdatedAt.Should().Be(actionTimestamp);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        const long nonExistentCategoryId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReparentCategoryRequest(
            NewParentCategoryId: (long?)null,
            RowVersion: 1);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUrl(nonExistentCategoryId));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenParentCategoryDoesNotExist()
    {
        var category = await Application.GivenCategoryAsync(
            name: $"Category-{TestToken}",
            slug: $"category-{TestToken}");

        const long nonExistentParentId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReparentCategoryRequest(
            NewParentCategoryId: nonExistentParentId,
            RowVersion: category.RowVersion);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUrl(category.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
    {
        var category = await Application.GivenCategoryAsync(
            name: $"Category-{TestToken}",
            slug: $"category-{TestToken}");

        var newParent = await Application.GivenCategoryAsync(
            name: $"NewParent-{TestToken}",
            slug: $"new-parent-{TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReparentCategoryRequest(
            NewParentCategoryId: newParent.Id,
            RowVersion: category.RowVersion + 1);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUrl(category.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsZero()
    {
        var category = await Application.GivenCategoryAsync(
            name: $"Category-{TestToken}",
            slug: $"category-{TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReparentCategoryRequest(
            NewParentCategoryId: (long?)null,
            RowVersion: 0);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUrl(category.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenCircularDependencyWouldOccur()
    {
        var parent = await Application.GivenCategoryAsync(
            name: $"Parent-{TestToken}",
            slug: $"parent-{TestToken}");

        var child = await Application.GivenCategoryAsync(
            name: $"Child-{TestToken}",
            slug: $"child-{TestToken}",
            parentCategoryId: parent.Id);

        var grandchild = await Application.GivenCategoryAsync(
            name: $"Grandchild-{TestToken}",
            slug: $"grandchild-{TestToken}",
            parentCategoryId: child.Id);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReparentCategoryRequest(
            NewParentCategoryId: grandchild.Id,
            RowVersion: parent.RowVersion);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUrl(parent.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
