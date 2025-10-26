using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductsSitemapData;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Public.GetProductsSitemapData;

public sealed class GetProductsSitemapDataEndpointTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/products/sitemap-data";

    public GetProductsSitemapDataEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task GetProductsSitemapDataEndpoint_ShouldRespondWith200Ok_AndSitemapDataForPublishedProductsUnderActiveCategories()
    {
        BuildApp();

        var adminClient = _app.RequestClient().UseAdminKey();

        var category = await adminClient.CreateCategoryAsync(
          name: "Bouquets",
          slug: $"bouquets-{_testId:N}"[..20],
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var created = await adminClient.CreateProductAsync(
          categoryId: category.Id,
          name: "Spring Bouquet",
          slug: $"spring-bouquet-{_testId:N}"[..20],
          publishStatus: PublishStatus.Published,
          availability: ProductAvailabilityStatus.Available,
          cancellationToken: TestContext.Current.CancellationToken);

        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync(EndpointUrl, TestContext.Current.CancellationToken);

        // Verify response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetProductsSitemapDataResponse>(TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();

        // Use domain information to verify response correctness
        var expectedProduct = await DbContext.Products
          .AsNoTracking()
          .SingleAsync(p => p.Id == created.Id, TestContext.Current.CancellationToken);
        payload.Items.Should().ContainSingle();
        var item = payload.Items.Single();
        item.Slug.Should().Be(expectedProduct.CurrentSlug);
        item.UpdatedOn.Should().Be(DateOnly.FromDateTime(expectedProduct.UpdatedAt.UtcDateTime));
    }
}
