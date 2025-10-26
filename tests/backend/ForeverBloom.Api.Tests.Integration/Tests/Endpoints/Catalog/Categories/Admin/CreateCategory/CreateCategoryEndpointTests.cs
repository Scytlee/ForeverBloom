using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.CreateCategory;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Helpers.Validation;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.CreateCategory;

public sealed class CreateCategoryEndpointTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/admin/categories";

    public CreateCategoryEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task CreateCategoryEndpoint_ShouldRespondWith400BadRequest_WhenNameNotUniqueWithinSameParent()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        var parentCategory = await client.CreateCategoryAsync(
          name: "Parent",
          slug: $"parent-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        const string childName = "Child";
        await client.CreateCategoryAsync(
          name: childName,
          slug: $"child-1-{_testId:N}"[..20],
          parentCategoryId: parentCategory.Id,
          cancellationToken: TestContext.Current.CancellationToken);

        var secondChild = new CreateCategoryRequest
        {
            Name = childName,
            Slug = $"child-2-{_testId:N}"[..20],
            ParentCategoryId = parentCategory.Id
        };
        var secondResponse = await client.PostAsJsonAsync(EndpointUrl, secondChild, TestContext.Current.CancellationToken);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await secondResponse.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(CreateCategoryRequest.Name), CategoryValidation.ErrorCodes.NameNotUniqueWithinParent);
    }

    [Fact]
    public async Task CreateCategoryEndpoint_ShouldRespondWith201Created_AndCategory_WhenCreationIsSuccessful()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        var slug = $"cat-{_testId:N}"[..20];
        var request = new CreateCategoryRequest
        {
            Name = "Complete Test Category",
            Slug = slug,
            Description = "This is a complete description of the test category with all fields populated",
            ImagePath = "/images/test-category.jpg",
            DisplayOrder = 10,
            IsActive = true
        };

        var response = await client.PostAsJsonAsync(EndpointUrl, request, TestContext.Current.CancellationToken);

        // Verify response
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseContent = await response.Content.ReadFromJsonAsync<CreateCategoryResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().EndWith($"{EndpointUrl}/{responseContent.Id}");

        // Verify database state
        var persistedCategory = await DbContext.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == responseContent.Id, TestContext.Current.CancellationToken);
        persistedCategory.Should().NotBeNull();
        persistedCategory.CreatedAt.Should().BeAfter(DateTimeOffset.MinValue);
        persistedCategory.UpdatedAt.Should().BeAfter(DateTimeOffset.MinValue);
        persistedCategory.CreatedAt.Should().BeExactly(persistedCategory.UpdatedAt);
        persistedCategory.DeletedAt.Should().BeNull();
        persistedCategory.RowVersion.Should().BeGreaterThan(0);
        persistedCategory.Path.ToString().Should().EndWith(request.Slug);
        AssertionHelpers.AssertAllPropertiesAreMapped(
          sourceObject: request,
          destinationObject: persistedCategory,
          overridesMap: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
          {
        { nameof(request.Slug), nameof(persistedCategory.CurrentSlug) }
          });

        // Verify response content
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

        // Verify slug registry entry
        var slugEntry = await DbContext.SlugRegistry.AsNoTracking().SingleOrDefaultAsync(s => s.Slug == request.Slug, TestContext.Current.CancellationToken);
        slugEntry.Should().NotBeNull();
        slugEntry.EntityType.Should().Be(EntityType.Category);
        slugEntry.EntityId.Should().Be(persistedCategory.Id);
        slugEntry.IsActive.Should().BeTrue();
    }
}
