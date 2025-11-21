using System.Net;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Admin.DeleteProduct;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Admin;

public sealed class DeleteProductEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.DeleteProduct;
    private static string EndpointUrl(long productId) => $"/api/v1/admin/products/{productId}";

    [Fact]
    public void Endpoint_ShouldMapToCorrectRoute()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveHttpVerb("DELETE");
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/products");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.DeleteProduct);
        ProductEndpointsModule.Names.DeleteProduct.Should().Be("DeleteProduct");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith204NoContent_WhenProductIsDeletedSuccessfully()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);
        // Simulate deletion one hour after grace period ends
        Fixture.TimeProvider.FastForwardBy(TimeSpan.FromHours(Product.DeletionGracePeriodInHours + 1));

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new DeleteProductRequest(product.RowVersion);
        var httpResponse = await client.DeleteAsync(
            $"{EndpointUrl(product.Id)}?RowVersion={request.RowVersion}",
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        const long nonExistentProductId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new DeleteProductRequest(RowVersion: 1);
        var httpResponse = await client.DeleteAsync(
            $"{EndpointUrl(nonExistentProductId)}?RowVersion={request.RowVersion}",
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

        var request = new DeleteProductRequest(product.RowVersion + 1);
        var httpResponse = await client.DeleteAsync(
            $"{EndpointUrl(product.Id)}?RowVersion={request.RowVersion}",
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

        var request = new DeleteProductRequest(RowVersion: 0);
        var httpResponse = await client.DeleteAsync(
            $"{EndpointUrl(product.Id)}?RowVersion={request.RowVersion}",
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenProductNotArchived()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(categoryId: category.Id);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new DeleteProductRequest(product.RowVersion);
        var httpResponse = await client.DeleteAsync(
            $"{EndpointUrl(product.Id)}?RowVersion={request.RowVersion}",
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenGracePeriodNotElapsed()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            isArchived: true);
        // Simulate deletion one hour BEFORE grace period ends
        Fixture.TimeProvider.FastForwardBy(TimeSpan.FromHours(Product.DeletionGracePeriodInHours - 1));

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new DeleteProductRequest(product.RowVersion);
        var httpResponse = await client.DeleteAsync(
            $"{EndpointUrl(product.Id)}?RowVersion={request.RowVersion}",
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
