using System.Net.Http.Json;
using System.Text.Json;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.ArchiveProduct;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProduct;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProductImages;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Tests.Support.Http;
using ForeverBloom.Domain.Shared.Models;

namespace ForeverBloom.Api.Tests.Helpers.Arrange;

/// <summary>
/// Arrange-only HTTP helpers for Product admin endpoints.
/// Use only for seeding and mutating state prior to Act assertions.
/// </summary>
public static class ProductArrangeHelpers
{
    private const string AdminBase = "/api/v1/admin/products";

    public static async Task<CreateProductResponse> CreateProductAsync(
      this HttpClient client,
      int categoryId,
      string? name = null,
      string? seoTitle = null,
      string? fullDescription = null,
      string? metaDescription = null,
      string? slug = null,
      decimal? price = null,
      int? displayOrder = null,
      bool? isFeatured = null,
      PublishStatus? publishStatus = null,
      ProductAvailabilityStatus? availability = null,
      CancellationToken cancellationToken = default)
    {
        var request = new CreateProductRequest
        {
            Name = name ?? $"Product-{Guid.NewGuid():N}"[..20],
            SeoTitle = seoTitle,
            FullDescription = fullDescription,
            MetaDescription = metaDescription,
            Slug = slug ?? $"prod-{Guid.NewGuid():N}"[..20],
            Price = price,
            DisplayOrder = displayOrder ?? 0,
            IsFeatured = isFeatured ?? false,
            PublishStatus = publishStatus ?? PublishStatus.Draft,
            Availability = availability ?? ProductAvailabilityStatus.Available,
            CategoryId = categoryId
        };

        var response = await client.PostAsJsonAsync(AdminBase, request, cancellationToken);
        await response.EnsureSuccessWithDiagnosticsAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CreateProductResponse>(cancellationToken))!;
    }

    public static async Task<UpdateProductResponse> UpdateProductAsync(
      this HttpClient client,
      int id,
      uint rowVersion,
      string? name = null,
      string? seoTitle = null,
      string? fullDescription = null,
      string? metaDescription = null,
      string? slug = null,
      decimal? price = null,
      int? categoryId = null,
      int? displayOrder = null,
      bool? isFeatured = null,
      PublishStatus? publishStatus = null,
      ProductAvailabilityStatus? availability = null,
      CancellationToken cancellationToken = default)
    {
        var request = new UpdateProductRequest
        {
            Name = name is not null ? Optional<string>.FromValue(name) : Optional<string>.Unset,
            SeoTitle = seoTitle is not null ? Optional<string?>.FromValue(seoTitle) : Optional<string?>.Unset,
            FullDescription = fullDescription is not null ? Optional<string?>.FromValue(fullDescription) : Optional<string?>.Unset,
            MetaDescription = metaDescription is not null ? Optional<string?>.FromValue(metaDescription) : Optional<string?>.Unset,
            Slug = slug is not null ? Optional<string>.FromValue(slug) : Optional<string>.Unset,
            Price = price is not null ? Optional<decimal?>.FromValue(price) : Optional<decimal?>.Unset,
            CategoryId = categoryId is not null ? Optional<int>.FromValue(categoryId.Value) : Optional<int>.Unset,
            DisplayOrder = displayOrder is not null ? Optional<int>.FromValue(displayOrder.Value) : Optional<int>.Unset,
            IsFeatured = isFeatured is not null ? Optional<bool>.FromValue(isFeatured.Value) : Optional<bool>.Unset,
            PublishStatus = publishStatus is not null ? Optional<PublishStatus>.FromValue(publishStatus.Value) : Optional<PublishStatus>.Unset,
            Availability = availability is not null ? Optional<ProductAvailabilityStatus>.FromValue(availability.Value) : Optional<ProductAvailabilityStatus>.Unset,
            RowVersion = rowVersion
        };

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, $"{AdminBase}/{id}");
        httpRequest.Content = JsonContent.Create(request, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, cancellationToken);
        await response.EnsureSuccessWithDiagnosticsAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<UpdateProductResponse>(cancellationToken))!;
    }

    public static async Task<UpdateProductImagesResponse> UpdateProductImagesAsync(
      this HttpClient client,
      int id,
      uint rowVersion,
      IReadOnlyList<UpdateProductImageItem>? images = null,
      CancellationToken cancellationToken = default)
    {
        var request = new UpdateProductImagesRequest
        {
            Images = images ?? [],
            RowVersion = rowVersion
        };

        var response = await client.PutAsJsonAsync($"{AdminBase}/{id}/images", request, cancellationToken);
        await response.EnsureSuccessWithDiagnosticsAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<UpdateProductImagesResponse>(cancellationToken))!;
    }

    public static async Task<ArchiveProductResponse> ArchiveProductAsync(
      this HttpClient client,
      int id,
      uint rowVersion,
      CancellationToken cancellationToken = default)
    {
        var request = new ArchiveProductRequest { RowVersion = rowVersion };
        var response = await client.PostAsJsonAsync($"{AdminBase}/{id}/archive", request, cancellationToken);
        await response.EnsureSuccessWithDiagnosticsAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<ArchiveProductResponse>(cancellationToken))!;
    }
}
