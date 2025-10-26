using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryBySlug;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Public.GetCategoryBySlug;

public sealed class GetCategoryBySlugEndpointTests : BackendAppTestClassBase
{
    private const string EndpointBaseUrl = "/api/v1/categories";

    public GetCategoryBySlugEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task GetCategoryBySlugEndpoint_ShouldRespondWith404NotFound_WhenSlugDoesNotExist()
    {
        BuildApp();

        var arrangeClient = _app.RequestClient().UseAdminKey();
        await arrangeClient.CreateCategoryAsync(
          name: "Garden",
          slug: $"garden-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync($"{EndpointBaseUrl}/unknown-slug", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCategoryBySlugEndpoint_ShouldRespondWith200Ok_AndCategory_WhenSlugIsCurrentAndCategoryIsActive()
    {
        BuildApp();

        var arrangeClient = _app.RequestClient().UseAdminKey();

        var parent = await arrangeClient.CreateCategoryAsync(
          name: "Flowers",
          slug: $"flowers-{_testId:N}"[..20],
          description: "All flower categories",
          displayOrder: 1,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var childSlug = $"roses-{_testId:N}"[..20];
        var child = await arrangeClient.CreateCategoryAsync(
          name: "Roses",
          slug: childSlug,
          parentCategoryId: parent.Id,
          description: "Red roses",
          imagePath: "/images/roses.png",
          displayOrder: 5,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync($"{EndpointBaseUrl}/{childSlug}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GetCategoryBySlugResponse>(TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();

        // Verify domain outcome: check database state
        var persistedCategory = await DbContext.Categories
          .AsNoTracking()
          .FirstOrDefaultAsync(c => c.Id == child.Id, TestContext.Current.CancellationToken);
        persistedCategory.Should().NotBeNull();

        // Verify entity to response mapping using assertion helper
        AssertionHelpers.AssertAllPropertiesAreMapped(
          sourceObject: persistedCategory,
          destinationObject: payload,
          overridesMap: new(StringComparer.OrdinalIgnoreCase)
          {
        { nameof(persistedCategory.CurrentSlug), nameof(payload.Slug) }
          },
          sourceExcludes: new(StringComparer.OrdinalIgnoreCase)
          {
        nameof(persistedCategory.DisplayOrder),
        nameof(persistedCategory.IsActive),
        nameof(persistedCategory.Path),
        nameof(persistedCategory.ParentCategory),
        nameof(persistedCategory.ChildCategories),
        nameof(persistedCategory.Products),
        nameof(persistedCategory.CreatedAt),
        nameof(persistedCategory.UpdatedAt),
        nameof(persistedCategory.RowVersion),
        nameof(persistedCategory.DeletedAt)
          },
          equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        payload.Breadcrumbs.Should().ContainInOrder(
          new BreadcrumbItem { Name = "Flowers", Slug = parent.Slug },
          new BreadcrumbItem { Name = "Roses", Slug = childSlug });
    }

    [Fact]
    public async Task GetCategoryBySlugEndpoint_ShouldRespondWith301MovedPermanently_AndLocationHeader_WhenSlugIsInactive()
    {
        BuildApp();

        var arrangeClient = _app.RequestClient().UseAdminKey();

        var originalSlug = $"orchids-{_testId:N}"[..20];
        var created = await arrangeClient.CreateCategoryAsync(
          name: "Orchids",
          slug: originalSlug,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var updatedSlug = $"orchids-new-{_testId:N}"[..20];

        var updated = await arrangeClient.UpdateCategoryAsync(
          created.Id,
          created.RowVersion,
          slug: updatedSlug,
          cancellationToken: TestContext.Current.CancellationToken);
        updated.Slug.Should().Be(updatedSlug);

        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync($"{EndpointBaseUrl}/{originalSlug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Be($"{EndpointBaseUrl}/{updatedSlug}");
    }
}
