using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.WebApi.Endpoints.Products;
using ForeverBloom.WebApi.Endpoints.Products.Admin.UpdateProductImages;
using ForeverBloom.WebApi.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.WebApi.IntegrationTests.Endpoints.Products.Admin;

public sealed class UpdateProductImagesEndpointTests : WebApiIntegrationTestBase
{
    private const string EndpointName = ProductEndpointsModule.Names.UpdateProductImages;
    private static string EndpointUrl(long productId) => $"/api/v1/admin/products/{productId}/images";

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

        endpoint.ShouldHaveName(ProductEndpointsModule.Names.UpdateProductImages);
        ProductEndpointsModule.Names.UpdateProductImages.Should().Be("UpdateProductImages");
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Products);
        endpoint.ShouldHaveTag(ProductEndpointsModule.Tags.Admin);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith200OkAndUpdatedImages_WhenValidRequest()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images:
            [
                new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1),
                new CreateProductCommandImage($"/images/{TestToken}-2.jpg", $"Image 2 {TestToken}", false, 2),
                new CreateProductCommandImage($"/images/{TestToken}-3.jpg", $"Image 3 {TestToken}", false, 3)
            ]);

        var firstImage = product.Images.ElementAt(0);
        var secondImage = product.Images.ElementAt(1);
        var thirdImage = product.Images.ElementAt(2);

        var client = CreateClient(WebApiKeyScope.Admin);

        var sourceForNewImage = $"/images/{TestToken}-new.jpg";
        var altTextForNewImage = $"New Image {TestToken}";
        var altTextForUpdatedImage = $"Updated Alt {TestToken}";
        var request = new UpdateProductImagesRequest(
            RowVersion: product.RowVersion,
            Create:
            [
                new UpdateProductImagesRequest.CreateImageOperation(
                    Source: sourceForNewImage,
                    AltText: altTextForNewImage,
                    IsPrimary: false,
                    DisplayOrder: 4)
            ],
            Update:
            [
                new UpdateProductImagesRequest.UpdateImageOperation(
                    Id: firstImage.Id,
                    AltText: altTextForUpdatedImage,
                    IsPrimary: Optional<bool>.Unset,
                    DisplayOrder: Optional<int>.Unset)
            ],
            Delete: [thirdImage.Id]);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(product.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await httpResponse.Content.ReadFromJsonAsync<UpdateProductImagesResponse>(CancellationToken);
        payload.Should().NotBeNull();
        payload.Images.Should().HaveCount(3);
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);

        var updatedImage = payload.Images.Single(i => i.Id == firstImage.Id);
        updatedImage.AltText.Should().Be(altTextForUpdatedImage);

        var unchangedImage = payload.Images.Single(i => i.Id == secondImage.Id);
        unchangedImage.Source.Should().Be(secondImage.Image.Source.Value);

        var newImage = payload.Images.Single(i => i.Source == sourceForNewImage);
        newImage.AltText.Should().Be(altTextForNewImage);

        payload.Images.Should().NotContain(i => i.Id == thirdImage.Id);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenRequestToCommandValidationFails()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(categoryId: category.Id);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductImagesRequest(
            RowVersion: product.RowVersion,
            Create:
            [
                new UpdateProductImagesRequest.CreateImageOperation(
                    Source: $"/images/{TestToken}-invalid.txt",
                    AltText: $"Invalid Image {TestToken}",
                    IsPrimary: true,
                    DisplayOrder: 1)
            ],
            Update: null,
            Delete: null);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(product.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith404NotFound_WhenProductDoesNotExist()
    {
        const long nonExistentProductId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductImagesRequest(
            RowVersion: 1,
            Create: null,
            Update: null,
            Delete: null);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(nonExistentProductId));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith409Conflict_WhenRowVersionMismatch()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images:
            [
                new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1)
            ]);

        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductImagesRequest(
            RowVersion: product.RowVersion + 1,
            Create: null,
            Update: null,
            Delete: null);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(product.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith400BadRequest_WhenImageNotFound()
    {
        var category = await Application.GivenCategoryAsync();
        var product = await Application.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images:
            [
                new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1)
            ]);

        const long nonExistentImageId = 999_999L;
        var client = CreateClient(WebApiKeyScope.Admin);

        var request = new UpdateProductImagesRequest(
            RowVersion: product.RowVersion,
            Create: null,
            Update:
            [
                new UpdateProductImagesRequest.UpdateImageOperation(
                    Id: nonExistentImageId,
                    AltText: $"New Alt {TestToken}",
                    IsPrimary: Optional<bool>.Unset,
                    DisplayOrder: Optional<int>.Unset)
            ],
            Delete: null);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(product.Id));
        httpRequest.Content = JsonContent.Create(request, options: GetJsonOptions());

        var httpResponse = await client.SendAsync(httpRequest, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
