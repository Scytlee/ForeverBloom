using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Admin.ReslugProduct;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Admin;

public sealed class ReslugProductEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.ReslugProduct;
    private static string EndpointUrl(long productId) => $"/api/v1/admin/products/{productId}:reslug";

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

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.ReslugProduct);
        ProductEndpointsModule.Names.ReslugProduct.Should().Be("ReslugProduct");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndReslugDetails_WhenProductExistsAndSlugIsValid()
    {
        var originalSlug = $"original-slug-{TestToken}";
        var newSlug = $"new-slug-{TestToken}";

        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            slug: originalSlug);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugProductRequest(
            RowVersion: product.RowVersion,
            NewSlug: newSlug);

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(product.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ReslugProductResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.CurrentSlug.Should().Be(newSlug);
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);
        payload.UpdatedAt.Should().Be(actionTimestamp);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRequestToCommandValidationFails()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            slug: $"original-slug-{TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugProductRequest(
            RowVersion: product.RowVersion,
            NewSlug: "INVALID_SLUG");

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(product.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        const long nonExistentProductId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugProductRequest(
            RowVersion: 1,
            NewSlug: $"does-not-matter-{TestToken}");

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
            slug: $"original-slug-{TestToken}");

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugProductRequest(
            RowVersion: product.RowVersion + 1,
            NewSlug: $"new-slug-{TestToken}");

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(product.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenSlugAlreadyInUse()
    {
        var existingSlug = $"existing-slug-{TestToken}";
        var targetSlug = $"target-slug-{TestToken}";

        var category = await Application.GivenCategoryAsync();

        await Application.GivenProductAsync(
            categoryId: category.Id,
            slug: existingSlug);

        var targetProduct = await Application.GivenProductAsync(
            categoryId: category.Id,
            slug: targetSlug);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new ReslugProductRequest(
            RowVersion: targetProduct.RowVersion,
            NewSlug: existingSlug);

        var httpResponse = await client.PostAsJsonAsync(
            EndpointUrl(targetProduct.Id),
            request,
            CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
