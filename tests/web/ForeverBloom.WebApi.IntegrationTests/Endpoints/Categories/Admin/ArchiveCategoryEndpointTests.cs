using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Admin.ArchiveCategory;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Admin;

public sealed class ArchiveCategoryEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.ArchiveCategory;
    private static string EndpointUrl(long categoryId) => $"/api/v1/admin/categories/{categoryId}:archive";

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

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.ArchiveCategory);
        CategoryEndpointsModule.Names.ArchiveCategory.Should().Be("ArchiveCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndArchivedCategory_WhenCategoryExists()
    {
        var category = await Application.GivenCategoryAsync();
        var client = CreateClient(WebApiKeyScope.Admin);
        var request = new ArchiveCategoryRequest(category.RowVersion);

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(category.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ArchiveCategoryResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.RowVersion.Should().BeGreaterThan(category.RowVersion);
        payload.DeletedAt.Should().Be(actionTimestamp);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        const long nonExistentCategoryId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ArchiveCategoryRequest(RowVersion: 1);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(nonExistentCategoryId),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenCategoryHasTooManyDescendants()
    {
        var rootCategory = await Application.GivenCategoryAsync(
            name: $"Limited-{TestToken}",
            slug: $"limited-{TestToken}");

        for (var i = 1; i <= Category.DescendantLimitOnUpdate + 1; i++)
        {
            await Application.GivenCategoryAsync(
                name: $"Child-{TestToken}-{i}",
                slug: $"child-{TestToken}-{i}",
                parentCategoryId: rootCategory.Id);
        }

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ArchiveCategoryRequest(rootCategory.RowVersion);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(rootCategory.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
    {
        var category = await Application.GivenCategoryAsync();
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ArchiveCategoryRequest(category.RowVersion + 1);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(category.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsZero()
    {
        var category = await Application.GivenCategoryAsync();
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ArchiveCategoryRequest(RowVersion: 0);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(category.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
