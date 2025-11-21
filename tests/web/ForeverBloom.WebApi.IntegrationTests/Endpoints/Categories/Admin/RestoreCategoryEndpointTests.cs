using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.Endpoints.Categories.Admin.RestoreCategory;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Admin;

public sealed class RestoreCategoryEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.RestoreCategory;
    private static string EndpointUrl(long categoryId) => $"/api/v1/admin/categories/{categoryId}:restore";

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

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.RestoreCategory);
        CategoryEndpointsModule.Names.RestoreCategory.Should().Be("RestoreCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndRestoredCategory_WhenArchivedCategoryExists()
    {
        var category = await Application.GivenCategoryAsync(
            isArchived: true);

        var client = CreateClient(WebApiKeyScope.Admin);
        var request = new RestoreCategoryRequest(category.RowVersion);

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(category.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<RestoreCategoryResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().BeGreaterThan(category.RowVersion);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        const long nonExistentCategoryId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new RestoreCategoryRequest(RowVersion: 1);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(nonExistentCategoryId),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenCategoryHasArchivedAncestors()
    {
        var parentCategory = await Application.GivenCategoryAsync();
        var childCategory = await Application.GivenCategoryAsync(
            parentCategoryId: parentCategory.Id);

        parentCategory = await Application.ArchiveExistingCategoryAsync(parentCategory);
        await ExecuteDbContextAsync((db, ct) => db.Entry(childCategory).ReloadAsync(ct));

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new RestoreCategoryRequest(childCategory.RowVersion);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(childCategory.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
    {
        var category = await Application.GivenCategoryAsync(
            isArchived: true);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new RestoreCategoryRequest(category.RowVersion + 1);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(category.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsZero()
    {
        var category = await Application.GivenCategoryAsync(
            isArchived: true);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new RestoreCategoryRequest(RowVersion: 0);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(category.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
