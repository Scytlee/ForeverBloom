using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Admin.RestoreProduct;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Admin;

public sealed class RestoreProductEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.RestoreProduct;
    private static string EndpointUrl(long productId) => $"/api/v1/admin/products/{productId}:restore";

    [Fact]
    public void Endpoint_ShouldMapToCorrectRoute()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("POST");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/products");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.RestoreProduct);
        ProductEndpointsModule.Names.RestoreProduct.Should().Be("RestoreProduct");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndRestoredProduct_WhenArchivedProductExists()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new RestoreProductRequest(product.RowVersion);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(product.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<RestoreProductResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        const long nonExistentProductId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new RestoreProductRequest(RowVersion: 1);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(nonExistentProductId),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new RestoreProductRequest(product.RowVersion + 1);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(product.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsZero()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new RestoreProductRequest(RowVersion: 0);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(product.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
