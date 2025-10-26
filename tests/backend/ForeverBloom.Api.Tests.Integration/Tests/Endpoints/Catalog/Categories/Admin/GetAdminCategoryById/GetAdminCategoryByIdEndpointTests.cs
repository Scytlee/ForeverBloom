using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.GetAdminCategoryById;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.GetAdminCategoryById;

public sealed class GetAdminCategoryByIdEndpointTests : BackendAppTestClassBase
{
    private const string EndpointBaseUrl = "/api/v1/admin/categories";

    public GetAdminCategoryByIdEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task GetAdminCategoryByIdEndpoint_ShouldRespondWith404NotFound_WhenIdDoesNotExist()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        var response = await client.GetAsync($"{EndpointBaseUrl}/999999", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAdminCategoryByIdEndpoint_ShouldRespondWith200Ok_AndCategoryDetails_WhenCategoryExists()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        var createdCategory = await client.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          description: "Complete category description",
          imagePath: "/images/category.jpg",
          displayOrder: 10,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"{EndpointBaseUrl}/{createdCategory.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<GetAdminCategoryByIdResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        var persistedCategory = await DbContext.Categories
          .AsNoTracking()
          .SingleAsync(c => c.Id == createdCategory.Id, TestContext.Current.CancellationToken);

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
    }
}
