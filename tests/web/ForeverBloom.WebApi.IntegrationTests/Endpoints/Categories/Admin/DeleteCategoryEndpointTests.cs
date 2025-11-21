using System.Net;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Categories;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Categories.Admin;

public sealed class DeleteCategoryEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = CategoryEndpointsModule.Names.DeleteCategory;
    private static string EndpointUrl(long categoryId, uint rowVersion)
        => $"/api/v1/admin/categories/{categoryId}?rowVersion={rowVersion}";

    [Fact]
    public void Endpoint_ShouldMapToCorrectRoute()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("DELETE");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/categories");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(CategoryEndpointsModule.Names.DeleteCategory);
        CategoryEndpointsModule.Names.DeleteCategory.Should().Be("DeleteCategory");
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Categories);
        endpoint.ShouldHaveTag(CategoryEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith204NoContent_WhenCategoryDeletedSuccessfully()
    {
        var category = await Application.GivenCategoryAsync(isArchived: true);

        var client = CreateClient(WebApiKeyScope.Admin);

        // Deletion occurs 1 hour after grace period ends
        Fixture.TimeProvider.FastForwardBy(TimeSpan.FromHours(Category.DeletionGracePeriodInHours + 1));
        var httpResponse = await client.DeleteAsync(
            EndpointUrl(category.Id, category.RowVersion),
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        const long nonExistentCategoryId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.DeleteAsync(
            EndpointUrl(nonExistentCategoryId, 1),
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
    {
        var category = await Application.GivenCategoryAsync(isArchived: true);

        var client = CreateClient(WebApiKeyScope.Admin);

        var staleRowVersion = category.RowVersion + 1;
        var httpResponse = await client.DeleteAsync(
            EndpointUrl(category.Id, staleRowVersion),
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsZero()
    {
        var category = await Application.GivenCategoryAsync();

        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.DeleteAsync(
            EndpointUrl(category.Id, 0),
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenCategoryNotArchived()
    {
        var category = await Application.GivenCategoryAsync();

        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.DeleteAsync(
            EndpointUrl(category.Id, category.RowVersion),
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
