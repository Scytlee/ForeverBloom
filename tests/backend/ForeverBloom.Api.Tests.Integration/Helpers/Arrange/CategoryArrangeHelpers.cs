using System.Net.Http.Json;
using System.Text.Json;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ArchiveCategory;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.CreateCategory;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.UpdateCategory;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Tests.Support.Http;

namespace ForeverBloom.Api.Tests.Helpers.Arrange;

/// <summary>
/// Arrange-only HTTP helpers for Category endpoints.
/// Use only for seeding state (create/mutate). Do not use in Act.
/// </summary>
public static class CategoryArrangeHelpers
{
    private const string AdminBase = "/api/v1/admin/categories";

    public static async Task<CreateCategoryResponse> CreateCategoryAsync(
      this HttpClient client,
      string? name = null,
      string? description = null,
      string? slug = null,
      string? imagePath = null,
      int? parentCategoryId = null,
      int? displayOrder = null,
      bool? isActive = null,
      CancellationToken cancellationToken = default)
    {
        var request = new CreateCategoryRequest
        {
            Name = name ?? $"Category-{Guid.NewGuid():N}"[..20],
            Description = description,
            Slug = slug ?? $"cat-{Guid.NewGuid():N}"[..20],
            ImagePath = imagePath,
            ParentCategoryId = parentCategoryId,
            DisplayOrder = displayOrder ?? 0,
            IsActive = isActive ?? true
        };

        var response = await client.PostAsJsonAsync(AdminBase, request, cancellationToken);
        await response.EnsureSuccessWithDiagnosticsAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CreateCategoryResponse>(cancellationToken))!;
    }

    public static async Task<UpdateCategoryResponse> UpdateCategoryAsync(
      this HttpClient client,
      int id,
      uint rowVersion,
      string? name = null,
      string? description = null,
      string? slug = null,
      string? imagePath = null,
      int? parentCategoryId = null,
      int? displayOrder = null,
      bool? isActive = null,
      CancellationToken cancellationToken = default)
    {
        var request = new UpdateCategoryRequest
        {
            Name = name is not null ? Optional<string>.FromValue(name) : Optional<string>.Unset,
            Description = description is not null ? Optional<string?>.FromValue(description) : Optional<string?>.Unset,
            Slug = slug is not null ? Optional<string>.FromValue(slug) : Optional<string>.Unset,
            ImagePath = imagePath is not null ? Optional<string?>.FromValue(imagePath) : Optional<string?>.Unset,
            ParentCategoryId = parentCategoryId is not null ? Optional<int?>.FromValue(parentCategoryId) : Optional<int?>.Unset,
            DisplayOrder = displayOrder is not null ? Optional<int>.FromValue(displayOrder.Value) : Optional<int>.Unset,
            IsActive = isActive is not null ? Optional<bool>.FromValue(isActive.Value) : Optional<bool>.Unset,
            RowVersion = rowVersion
        };

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, $"{AdminBase}/{id}");
        httpRequest.Content = JsonContent.Create(request, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, cancellationToken);
        await response.EnsureSuccessWithDiagnosticsAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<UpdateCategoryResponse>(cancellationToken))!;
    }

    public static async Task<ArchiveCategoryResponse> ArchiveCategoryAsync(
      this HttpClient client,
      int id,
      uint rowVersion,
      CancellationToken cancellationToken = default)
    {
        var request = new ArchiveCategoryRequest { RowVersion = rowVersion };
        var response = await client.PostAsJsonAsync($"{AdminBase}/{id}/archive", request, cancellationToken);
        await response.EnsureSuccessWithDiagnosticsAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<ArchiveCategoryResponse>(cancellationToken))!;
    }
}
