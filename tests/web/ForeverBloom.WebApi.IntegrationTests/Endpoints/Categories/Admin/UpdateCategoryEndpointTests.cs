using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Admin.UpdateCategory;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Admin;

public sealed class UpdateCategoryEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.UpdateCategory;
    private static string EndpointUrl(long categoryId) => $"/api/v1/admin/categories/{categoryId}";

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

        endpoint.ShouldHaveHttpVerb("PATCH");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/categories");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.UpdateCategory);
        CategoryEndpointsModule.Names.UpdateCategory.Should().Be("UpdateCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndUpdatedCategory_WhenValidRequest()
    {
        var category = await Application.GivenCategoryAsync(
            name: $"Original-{TestToken}",
            slug: $"original-{TestToken}",
            description: $"Original description {TestToken}",
            imagePath: $"/images/{TestToken}-old.jpg",
            imageAltText: $"Old image {TestToken}",
            displayOrder: 5,
            publishStatus: PublishStatus.Draft);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateCategoryRequest(
            RowVersion: category.RowVersion,
            Name: $"Updated-{TestToken}",
            Description: $"Updated description {TestToken}",
            ImagePath: $"/images/{TestToken}-new.jpg",
            ImageAltText: $"New image {TestToken}",
            DisplayOrder: 10,
            PublishStatus: PublishStatus.Published.Name);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(category.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<UpdateCategoryResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Name.Should().Be($"Updated-{TestToken}");
        payload.Description.Should().Be($"Updated description {TestToken}");
        payload.ImagePath.Should().Be($"/images/{TestToken}-new.jpg");
        payload.ImageAltText.Should().Be($"New image {TestToken}");
        payload.DisplayOrder.Should().Be(10);
        payload.PublishStatus.Should().Be(PublishStatus.Published.Name);
        payload.RowVersion.Should().BeGreaterThan(category.RowVersion);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        const long nonExistentCategoryId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateCategoryRequest(
            RowVersion: 1,
            Name: $"DoesNotMatter-{TestToken}",
            Description: Optional<string?>.Unset,
            ImagePath: Optional<string?>.Unset,
            ImageAltText: Optional<string?>.Unset,
            DisplayOrder: Optional<int>.Unset,
            PublishStatus: Optional<string>.Unset);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(nonExistentCategoryId));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
    {
        var category = await Application.GivenCategoryAsync(
            name: $"Stale-{TestToken}",
            slug: $"stale-{TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateCategoryRequest(
            RowVersion: category.RowVersion + 1,
            Name: $"Updated-{TestToken}",
            Description: Optional<string?>.Unset,
            ImagePath: Optional<string?>.Unset,
            ImageAltText: Optional<string?>.Unset,
            DisplayOrder: Optional<int>.Unset,
            PublishStatus: Optional<string>.Unset);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(category.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsZero()
    {
        var category = await Application.GivenCategoryAsync(
            name: $"Test-{TestToken}",
            slug: $"test-{TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateCategoryRequest(
            RowVersion: 0,
            Name: $"Updated-{TestToken}",
            Description: Optional<string?>.Unset,
            ImagePath: Optional<string?>.Unset,
            ImageAltText: Optional<string?>.Unset,
            DisplayOrder: Optional<int>.Unset,
            PublishStatus: Optional<string>.Unset);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(category.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenPublishStatusTransitionNotAllowed()
    {
        var category = await Application.GivenCategoryAsync(
            name: $"Published-{TestToken}",
            slug: $"published-{TestToken}",
            publishStatus: PublishStatus.Published);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateCategoryRequest(
            RowVersion: category.RowVersion,
            Name: Optional<string>.Unset,
            Description: Optional<string?>.Unset,
            ImagePath: Optional<string?>.Unset,
            ImageAltText: Optional<string?>.Unset,
            DisplayOrder: Optional<int>.Unset,
            PublishStatus: PublishStatus.Draft.Name);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(category.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
