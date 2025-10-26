using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.ListProducts;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Validation;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Public.ListProducts;

public sealed class ListProductsEndpointTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/products";

    public ListProductsEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task ListProductsEndpoint_ShouldRespondWith200Ok_AndPublishedProductsWithDefaultSorting_WhenRequestIsSuccessful()
    {
        BuildApp();
        var adminClient = _app.RequestClient().UseAdminKey();
        var category = await adminClient.CreateCategoryAsync(
          name: "Bouquets",
          slug: $"bouquets-{_testId:N}"[..20],
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);
        await adminClient.CreateProductAsync(
          categoryId: category.Id,
          name: "Autumn Arrangement",
          slug: $"autumn-{_testId:N}"[..20],
          publishStatus: PublishStatus.Published,
          availability: ProductAvailabilityStatus.Available,
          displayOrder: 2,
          cancellationToken: TestContext.Current.CancellationToken);
        await adminClient.CreateProductAsync(
          categoryId: category.Id,
          name: "Alpine Tulips",
          slug: $"alpine-{_testId:N}"[..20],
          publishStatus: PublishStatus.Published,
          availability: ProductAvailabilityStatus.ComingSoon,
          displayOrder: 1,
          cancellationToken: TestContext.Current.CancellationToken);
        await adminClient.CreateProductAsync(
          categoryId: category.Id,
          name: "Draft Item",
          slug: $"draft-{_testId:N}"[..20],
          publishStatus: PublishStatus.Draft,
          availability: ProductAvailabilityStatus.Available,
          cancellationToken: TestContext.Current.CancellationToken);

        var client = _app.RequestClient().UseFrontendKey();
        var response = await client.GetAsync(EndpointUrl, TestContext.Current.CancellationToken);

        // Verify response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ListProductsResponse>(TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();

        // Use domain information to verify response correctness
        var expectedProducts = await DbContext.Products
          .AsNoTracking()
          .Include(p => p.Category)
          .Include(p => p.Images)
          .Where(p => p.PublishStatus == PublishStatus.Published && p.Category.IsActive)
          .OrderBy(p => p.DisplayOrder)
          .ThenBy(p => p.Name)
          .ToListAsync(TestContext.Current.CancellationToken);

        // Assert paging metadata
        payload.TotalCount.Should().Be(expectedProducts.Count);
        payload.PageNumber.Should().Be(1);
        payload.PageSize.Should().Be(20);
        payload.HasNextPage.Should().BeFalse();
        payload.Items.Should().HaveCount(expectedProducts.Count);

        // Assert ordering matches expected (DisplayOrder asc, Name asc)
        payload.Items.Select(p => p.Id).Should().ContainInOrder(expectedProducts.Select(p => p.Id));

        // Assert key mapped fields for verification (Id, Slug, CategoryName, PrimaryImagePath)
        var firstItem = payload.Items.First();
        var firstExpected = expectedProducts.First();
        firstItem.Id.Should().Be(firstExpected.Id);
        firstItem.Slug.Should().Be(firstExpected.CurrentSlug);
        firstItem.CategoryName.Should().Be(firstExpected.Category.Name);
        firstItem.PrimaryImagePath.Should().Be(firstExpected.Images.FirstOrDefault(i => i.IsPrimary)?.ImagePath);
    }

    [Fact]
    public async Task ListProductsEndpoint_ShouldRespondWith400BadRequest_WhenOrderByParameterIsInvalid()
    {
        BuildApp();
        var client = _app.RequestClient().UseFrontendKey();

        var response = await client.GetAsync($"{EndpointUrl}?orderBy=UnknownColumn", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(ListProductsRequest.OrderBy), ProductValidation.ErrorCodes.InvalidSortParameters);
    }
}
