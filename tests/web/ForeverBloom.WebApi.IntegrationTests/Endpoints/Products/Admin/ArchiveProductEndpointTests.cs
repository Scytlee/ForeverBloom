using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Admin.ArchiveProduct;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Admin;

public sealed class ArchiveProductEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.ArchiveProduct;
    private static string EndpointUrl(long productId) => $"admin/products/{productId}:archive";

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

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.ArchiveProduct);
        ProductEndpointsModule.Names.ArchiveProduct.Should().Be("ArchiveProduct");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndArchivedProduct_WhenProductExists()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(categoryId: category.Id);
        var client = CreateClient(WebApiKeyScope.Admin);
        var request = new ArchiveProductRequest(product.RowVersion);

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(product.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ArchiveProductResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);
        payload.DeletedAt.Should().Be(actionTimestamp);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        const long nonExistentProductId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ArchiveProductRequest(RowVersion: 1);
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
        var product = await Application.GivenProductAsync(categoryId: category.Id);
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ArchiveProductRequest(product.RowVersion + 1);
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
        var product = await Application.GivenProductAsync(categoryId: category.Id);
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ArchiveProductRequest(RowVersion: 0);
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(product.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
