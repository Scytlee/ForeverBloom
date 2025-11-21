using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Admin.GetCategoryById;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Admin;

public sealed class GetCategoryByIdEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.GetCategoryById;
    private static string EndpointUrl(long categoryId) => $"/api/v1/admin/categories/{categoryId}";

    [Fact]
    public void Endpoint_ShouldMapToCorrectRoute()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("GET");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/categories");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.GetCategoryById);
        CategoryEndpointsModule.Names.GetCategoryById.Should().Be("GetCategoryById");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndCategoryDetails_WhenCategoryExists()
    {
        var name = $"Test Category {TestToken}";
        var slug = $"test-category-{TestToken}";
        var description = $"Test category description {TestToken}";
        var imagePath = $"/images/category-{TestToken}.jpg";
        var imageAltText = $"Category image {TestToken}";
        const int displayOrder = 5;

        var category = await Application.GivenCategoryAsync(
            name: name,
            slug: slug,
            description: description,
            imagePath: imagePath,
            imageAltText: imageAltText,
            displayOrder: displayOrder,
            publishStatus: PublishStatus.Published);
        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.GetAsync(EndpointUrl(category.Id), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<GetCategoryByIdResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Id.Should().Be(category.Id);
        payload.Name.Should().Be(name);
        payload.Description.Should().Be(description);
        payload.Slug.Should().Be(slug);
        payload.ImagePath.Should().Be(imagePath);
        payload.ImageAltText.Should().Be(imageAltText);
        payload.DisplayOrder.Should().Be(displayOrder);
        payload.PublishStatus.Should().Be("published");
        payload.Path.Should().Be(slug);
        payload.ParentCategoryId.Should().BeNull();
        payload.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenIdIsInvalid()
    {
        const long invalidId = 0;
        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.GetAsync(EndpointUrl(invalidId), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        const long nonExistentId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.GetAsync(EndpointUrl(nonExistentId), CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
