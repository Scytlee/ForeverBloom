using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.UpdateCategory;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Helpers.Validation;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.UpdateCategory;

public sealed class UpdateCategoryEndpointTests : BackendAppTestClassBase
{
    private static string EndpointUrl(int categoryId) => $"/api/v1/admin/categories/{categoryId}";

    public UpdateCategoryEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    private static UpdateCategoryRequest BuildUpdateCategoryRequest(
      Optional<string>? name = null,
      Optional<string?>? description = null,
      Optional<string>? slug = null,
      Optional<string?>? imagePath = null,
      Optional<int?>? parentCategoryId = null,
      Optional<int>? displayOrder = null,
      Optional<bool>? isActive = null,
      uint? rowVersion = null)
    {
        return new UpdateCategoryRequest
        {
            Name = name ?? Optional<string>.Unset,
            Description = description ?? Optional<string?>.Unset,
            Slug = slug ?? Optional<string>.Unset,
            ImagePath = imagePath ?? Optional<string?>.Unset,
            ParentCategoryId = parentCategoryId ?? Optional<int?>.Unset,
            DisplayOrder = displayOrder ?? Optional<int>.Unset,
            IsActive = isActive ?? Optional<bool>.Unset,
            RowVersion = rowVersion ?? 1
        };
    }

    [Fact]
    public async Task UpdateCategoryEndpoint_ShouldRespondWith200Ok_WhenUpdateIsSuccessful()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Original-{_testId:N}"[..20],
          slug: $"orig-{_testId:N}"[..20],
          description: "Original description",
          imagePath: "/images/original.jpg",
          displayOrder: 5,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        // Act: Update the category
        var updateRequest = BuildUpdateCategoryRequest(
          name: Optional<string>.FromValue($"Updated-{_testId:N}"[..20]),
          slug: Optional<string>.FromValue($"updated-{_testId:N}"[..20]),
          description: Optional<string?>.FromValue("Updated description"),
          imagePath: Optional<string?>.FromValue("/images/updated.jpg"),
          displayOrder: Optional<int>.FromValue(10),
          isActive: Optional<bool>.FromValue(false),
          rowVersion: createdCategory.RowVersion);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(createdCategory.Id));
        httpRequest.Content = JsonContent.Create(updateRequest, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // Assert response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<UpdateCategoryResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        // Assert database state
        var persistedCategory = await DbContext.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == createdCategory.Id, TestContext.Current.CancellationToken);
        persistedCategory.Should().NotBeNull();
        persistedCategory.UpdatedAt.Should().BeAfter(persistedCategory.CreatedAt);
        persistedCategory.RowVersion.Should().BeGreaterThan(createdCategory.RowVersion);

        // Verify specific field updates
        persistedCategory.Name.Should().Be(updateRequest.Name.Value);
        persistedCategory.CurrentSlug.Should().Be(updateRequest.Slug.Value);
        persistedCategory.Path.ToString().Should().Be(updateRequest.Slug.Value); // Root category, so path == slug
        persistedCategory.Description.Should().Be(updateRequest.Description.Value);
        persistedCategory.ImagePath.Should().Be(updateRequest.ImagePath.Value);
        persistedCategory.DisplayOrder.Should().Be(updateRequest.DisplayOrder.Value);
        persistedCategory.IsActive.Should().Be(updateRequest.IsActive.Value);

        // Verify response content maps correctly to persisted entity
        AssertionHelpers.AssertAllPropertiesAreMapped(
          sourceObject: persistedCategory,
          destinationObject: responseContent,
          overridesMap: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
          {
        { nameof(persistedCategory.CurrentSlug), nameof(responseContent.Slug) }
          },
          sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
          {
        nameof(persistedCategory.Path),
        nameof(persistedCategory.ParentCategory),
        nameof(persistedCategory.ChildCategories),
        nameof(persistedCategory.Products)
          },
          equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        // Verify slug registry updated
        var newSlugEntry = await DbContext.SlugRegistry.AsNoTracking().SingleAsync(s => s.Slug == updateRequest.Slug.Value, TestContext.Current.CancellationToken);
        newSlugEntry.EntityType.Should().Be(EntityType.Category);
        newSlugEntry.EntityId.Should().Be(createdCategory.Id);
        newSlugEntry.IsActive.Should().BeTrue();

        // Verify old slug registry entry is deactivated
        var oldSlugEntry = await DbContext.SlugRegistry.AsNoTracking().SingleAsync(s => s.Slug == createdCategory.Slug, TestContext.Current.CancellationToken);
        oldSlugEntry.Should().NotBeNull();
        oldSlugEntry.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCategoryEndpoint_ShouldRespondWith400BadRequest_WhenSlugChangeAttemptedOnCategoryWithChildren()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create parent category
        var parentCategory = await client.CreateCategoryAsync(
          name: $"Parent-{_testId:N}"[..20],
          slug: $"parent-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Create child category
        await client.CreateCategoryAsync(
          name: $"Child-{_testId:N}"[..20],
          slug: $"child-{_testId:N}"[..20],
          parentCategoryId: parentCategory.Id,
          cancellationToken: TestContext.Current.CancellationToken);

        // Act: Try to update parent category's slug (should fail due to children)
        var updateRequest = BuildUpdateCategoryRequest(
          slug: Optional<string>.FromValue($"new-parent-{_testId:N}"[..20]),
          rowVersion: parentCategory.RowVersion);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(parentCategory.Id));
        httpRequest.Content = JsonContent.Create(updateRequest, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(UpdateCategoryRequest.Slug), CategoryValidation.ErrorCodes.HierarchyChangeNotAllowed);
    }

    [Fact]
    public async Task UpdateCategoryEndpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Act: Try to update non-existent category
        var updateRequest = BuildUpdateCategoryRequest(
          name: Optional<string>.FromValue($"Updated-{_testId:N}"[..20]),
          rowVersion: 1);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(999999));
        httpRequest.Content = JsonContent.Create(updateRequest, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategoryEndpoint_ShouldRespondWith409Conflict_WhenConcurrentUpdateOccurs()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Simulate concurrent update by modifying the category directly in the database
        var category = await DbContext.Categories.SingleAsync(c => c.Id == createdCategory.Id, TestContext.Current.CancellationToken);
        category.Name = "Concurrently Modified";
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: Try to update with stale row version
        var updateRequest = BuildUpdateCategoryRequest(
          name: Optional<string>.FromValue($"My-Update-{_testId:N}"[..20]),
          rowVersion: createdCategory.RowVersion); // This is now stale

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.TypeInfoResolverChain.Add(new OptionalJsonTypeInfoResolver());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, EndpointUrl(createdCategory.Id));
        httpRequest.Content = JsonContent.Create(updateRequest, options: jsonOptions);

        var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
