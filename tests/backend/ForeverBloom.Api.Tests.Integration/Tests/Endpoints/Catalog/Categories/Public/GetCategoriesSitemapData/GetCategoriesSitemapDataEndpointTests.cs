using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoriesSitemapData;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Testing.Common.Fixtures.Database;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Public.GetCategoriesSitemapData;

public sealed class GetCategoriesSitemapDataEndpointTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/categories/sitemap-data";

    public GetCategoriesSitemapDataEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task GetCategoriesSitemapDataEndpoint_ShouldRespondWith200Ok_AndSitemapDataForActiveCategories_WhenRequestIsSuccessful()
    {
        BuildApp();

        var arrangeClient = _app.RequestClient().UseAdminKey();

        var firstCategory = await arrangeClient.CreateCategoryAsync(
          name: "First Category",
          slug: $"first-{_testId:N}"[..20],
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var secondCategory = await arrangeClient.CreateCategoryAsync(
          name: "Second Category",
          slug: $"second-{_testId:N}"[..20],
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync(EndpointUrl, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GetCategoriesSitemapDataResponse>(TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();
        payload.Items.Should().HaveCount(2);
        payload.Items.Should().Contain(item => item.Slug == firstCategory.Slug);
        payload.Items.Should().Contain(item => item.Slug == secondCategory.Slug);
        payload.Items.Should().OnlyContain(item => !string.IsNullOrEmpty(item.Slug) && item.UpdatedOn > DateOnly.MinValue);
    }
}
