using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryTree;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Public.GetCategoryTree;

public sealed class GetCategoryTreeEndpointTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/categories/tree";

    public GetCategoryTreeEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task GetCategoryTreeEndpoint_ShouldRespondWith200Ok_AndCompleteHierarchy_WhenCategoriesExist()
    {
        BuildApp();

        var arrangeClient = _app.RequestClient().UseAdminKey();

        var root = await arrangeClient.CreateCategoryAsync(
          name: "Flowers",
          slug: $"flowers-{_testId:N}"[..20],
          imagePath: "/images/flowers.png",
          displayOrder: 1,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var roses = await arrangeClient.CreateCategoryAsync(
          name: "Roses",
          slug: $"roses-{_testId:N}"[..20],
          parentCategoryId: root.Id,
          displayOrder: 1,
          imagePath: "/images/roses.png",
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var tulips = await arrangeClient.CreateCategoryAsync(
          name: "Tulips",
          slug: $"tulips-{_testId:N}"[..20],
          parentCategoryId: root.Id,
          displayOrder: 2,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var hybrid = await arrangeClient.CreateCategoryAsync(
          name: "Hybrid Roses",
          slug: $"hybrid-roses-{_testId:N}"[..20],
          parentCategoryId: roses.Id,
          displayOrder: 1,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync(EndpointUrl, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GetCategoryTreeResponse>(TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();
        payload.Categories.Should().HaveCount(1);

        // Use domain information to verify response correctness
        var persistedCategories = await DbContext.Categories
          .AsNoTracking()
          .Where(c => new[] { root.Id, roses.Id, tulips.Id, hybrid.Id }.Contains(c.Id))
          .ToDictionaryAsync(c => c.Id, TestContext.Current.CancellationToken);

        payload.Categories.Should().BeEquivalentTo([
          new CategoryTreeItem(
        persistedCategories[root.Id].Id,
        persistedCategories[root.Id].Name,
        persistedCategories[root.Id].CurrentSlug,
        persistedCategories[root.Id].ImagePath,
        [
          new CategoryTreeItem(
            persistedCategories[roses.Id].Id,
            persistedCategories[roses.Id].Name,
            persistedCategories[roses.Id].CurrentSlug,
            persistedCategories[roses.Id].ImagePath,
            [
              new CategoryTreeItem(
                persistedCategories[hybrid.Id].Id,
                persistedCategories[hybrid.Id].Name,
                persistedCategories[hybrid.Id].CurrentSlug,
                persistedCategories[hybrid.Id].ImagePath,
                [])
            ]),
          new CategoryTreeItem(
            persistedCategories[tulips.Id].Id,
            persistedCategories[tulips.Id].Name,
            persistedCategories[tulips.Id].CurrentSlug,
            persistedCategories[tulips.Id].ImagePath,
            [])
        ])
        ], options => options.WithStrictOrdering());
    }
}
