using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Admin.UpdateProduct;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Admin;

public sealed class UpdateProductEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.UpdateProduct;
    private static string EndpointUrl(long productId) => $"/api/v1/admin/products/{productId}";

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
        endpoint.ShouldHaveRoutePrefix("/api/v1/admin/products");
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectMetadata()
    {
        using var scope = Fixture.CreateServiceScope();
        var serviceProvider = scope.ServiceProvider;
        var dataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        var endpoint = EndpointMetadataTestHelper.GetRequiredEndpointByName(dataSource, EndpointName);

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.UpdateProduct);
        ProductEndpointsModule.Names.UpdateProduct.Should().Be("UpdateProduct");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndUpdatedProduct_WhenValidRequest()
    {
        var originalCategory = await Application.GivenCategoryAsync();
        var targetCategory = await Application.GivenCategoryAsync();

        var product = await Application.GivenProductAsync(
            categoryId: originalCategory.Id,
            name: $"Original-{TestToken}",
            price: 49.99m,
            isFeatured: false);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductRequest(
            RowVersion: product.RowVersion,
            Name: Optional<string>.FromValue($"Updated-{TestToken}"),
            SeoTitle: Optional<string?>.FromValue($"Updated Seo {TestToken}"),
            FullDescription: Optional<string?>.Unset,
            MetaDescription: Optional<string?>.Unset,
            CategoryId: Optional<long>.FromValue(targetCategory.Id),
            Price: Optional<decimal?>.FromValue(199.95m),
            IsFeatured: Optional<bool>.FromValue(true),
            Availability: Optional<string>.FromValue("available"),
            PublishStatus: Optional<string>.Unset);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(product.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<UpdateProductResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Name.Should().Be($"Updated-{TestToken}");
        payload.CategoryId.Should().Be(targetCategory.Id);
        payload.Price.Should().Be(199.95m);
        payload.IsFeatured.Should().BeTrue();
        payload.Availability.Should().Be("available");
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        const long nonExistentProductId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductRequest(
            RowVersion: 1,
            Name: Optional<string>.FromValue($"DoesNotMatter-{TestToken}"),
            SeoTitle: Optional<string?>.Unset,
            FullDescription: Optional<string?>.Unset,
            MetaDescription: Optional<string?>.Unset,
            CategoryId: Optional<long>.Unset,
            Price: Optional<decimal?>.Unset,
            IsFeatured: Optional<bool>.Unset,
            Availability: Optional<string>.Unset,
            PublishStatus: Optional<string>.Unset);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(nonExistentProductId));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenCategoryNotFound()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(categoryId: category.Id);

        const long nonExistentCategoryId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductRequest(
            RowVersion: product.RowVersion,
            Name: Optional<string>.Unset,
            SeoTitle: Optional<string?>.Unset,
            FullDescription: Optional<string?>.Unset,
            MetaDescription: Optional<string?>.Unset,
            CategoryId: Optional<long>.FromValue(nonExistentCategoryId),
            Price: Optional<decimal?>.Unset,
            IsFeatured: Optional<bool>.Unset,
            Availability: Optional<string>.Unset,
            PublishStatus: Optional<string>.Unset);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(product.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(categoryId: category.Id);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductRequest(
            RowVersion: product.RowVersion + 1,
            Name: Optional<string>.FromValue($"Updated-{TestToken}"),
            SeoTitle: Optional<string?>.Unset,
            FullDescription: Optional<string?>.Unset,
            MetaDescription: Optional<string?>.Unset,
            CategoryId: Optional<long>.Unset,
            Price: Optional<decimal?>.Unset,
            IsFeatured: Optional<bool>.Unset,
            Availability: Optional<string>.Unset,
            PublishStatus: Optional<string>.Unset);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(product.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsZero()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(categoryId: category.Id);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductRequest(
            RowVersion: 0,
            Name: Optional<string>.FromValue($"Updated-{TestToken}"),
            SeoTitle: Optional<string?>.Unset,
            FullDescription: Optional<string?>.Unset,
            MetaDescription: Optional<string?>.Unset,
            CategoryId: Optional<long>.Unset,
            Price: Optional<decimal?>.Unset,
            IsFeatured: Optional<bool>.Unset,
            Availability: Optional<string>.Unset,
            PublishStatus: Optional<string>.Unset);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(product.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenPublishStatusTransitionNotAllowed()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            publishStatus: PublishStatus.Published);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductRequest(
            RowVersion: product.RowVersion,
            Name: Optional<string>.Unset,
            SeoTitle: Optional<string?>.Unset,
            FullDescription: Optional<string?>.Unset,
            MetaDescription: Optional<string?>.Unset,
            CategoryId: Optional<long>.Unset,
            Price: Optional<decimal?>.Unset,
            IsFeatured: Optional<bool>.Unset,
            Availability: Optional<string>.Unset,
            PublishStatus: Optional<string>.FromValue("draft"));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(product.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
