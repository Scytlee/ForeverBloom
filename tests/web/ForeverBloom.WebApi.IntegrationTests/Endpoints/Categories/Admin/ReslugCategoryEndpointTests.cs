using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Admin.ReslugCategory;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Admin;

public sealed class ReslugCategoryEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.ReslugCategory;
    private static string EndpointUrl(long categoryId) => $"/api/v1/admin/categories/{categoryId}:reslug";

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

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.ReslugCategory);
        CategoryEndpointsModule.Names.ReslugCategory.Should().Be("ReslugCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndResluggedCategory_WhenValidRequest()
    {
        var originalSlug = $"original-{TestToken}";
        var newSlug = $"updated-{TestToken}";

        var category = await Application.GivenCategoryAsync(
            slug: originalSlug);
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugCategoryRequest(
            NewSlug: newSlug,
            RowVersion: category.RowVersion);

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(category.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ReslugCategoryResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Slug.Should().Be(newSlug);
        payload.Path.Should().Be(newSlug);
        payload.RowVersion.Should().BeGreaterThan(category.RowVersion);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsZero()
    {
        var category = await Application.GivenCategoryAsync();
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugCategoryRequest(
            NewSlug: $"new-slug-{TestToken}",
            RowVersion: 0);

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(category.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        const long nonExistentCategoryId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugCategoryRequest(
            NewSlug: $"new-slug-{TestToken}",
            RowVersion: 1);

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(nonExistentCategoryId),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenSlugNotAvailable()
    {
        var existingSlug = $"existing-{TestToken}";
        await Application.GivenCategoryAsync(
            slug: existingSlug);

        var targetCategory = await Application.GivenCategoryAsync(
            slug: $"target-{TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugCategoryRequest(
            NewSlug: existingSlug,
            RowVersion: targetCategory.RowVersion);

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(targetCategory.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
    {
        var category = await Application.GivenCategoryAsync();
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugCategoryRequest(
            NewSlug: $"new-slug-{TestToken}",
            RowVersion: category.RowVersion + 1);

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(category.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenCategoryHasTooManyDescendants()
    {
        var rootCategory = await Application.GivenCategoryAsync(
            slug: $"limited-{TestToken}");

        for (var i = 1; i <= Category.DescendantLimitOnUpdate + 1; i++)
        {
            await Application.GivenCategoryAsync(
                name: $"Child-{i}-{TestToken}",
                slug: $"child-{i}-{TestToken}",
                parentCategoryId: rootCategory.Id);
        }

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugCategoryRequest(
            NewSlug: $"limited-updated-{TestToken}",
            RowVersion: rootCategory.RowVersion);

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(rootCategory.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
