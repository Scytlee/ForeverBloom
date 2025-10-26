using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.ListAdminProducts;
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

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.Admin.ListAdminProducts;

public sealed class ListAdminProductsEndpointTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/admin/products";

    public ListAdminProductsEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task ListAdminProductsEndpoint_ShouldRespondWith200Ok_AndPaginatedProducts_WhenRequestIsValid()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        var category = await client.CreateCategoryAsync(
            name: $"Category-{_testId:N}"[..20],
            slug: $"cat-{_testId:N}"[..20],
            displayOrder: 1,
            isActive: true,
            cancellationToken: TestContext.Current.CancellationToken);

        var firstProduct = await client.CreateProductAsync(
            categoryId: category.Id,
            name: $"Product-{_testId:N}"[..20],
            slug: $"prod-{_testId:N}"[..20],
            publishStatus: PublishStatus.Published,
            availability: ProductAvailabilityStatus.Available,
            displayOrder: 2,
            price: 99.99m,
            cancellationToken: TestContext.Current.CancellationToken);

        var secondProduct = await client.CreateProductAsync(
            categoryId: category.Id,
            name: $"AnotherProd-{_testId:N}"[..20],
            slug: $"another-{_testId:N}"[..20],
            publishStatus: PublishStatus.Draft,
            availability: ProductAvailabilityStatus.ComingSoon,
            displayOrder: 1,
            price: 149.99m,
            cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"{EndpointUrl}?pageNumber=1&pageSize=10", TestContext.Current.CancellationToken);

        // Verify response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<ListAdminProductsResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        // Verify paging metadata
        responseContent.PageNumber.Should().Be(1);
        responseContent.PageSize.Should().Be(10);
        responseContent.TotalCount.Should().BeGreaterOrEqualTo(2);
        responseContent.Items.Should().HaveCountGreaterOrEqualTo(2);

        // Verify database state mapping
        var persistedProducts = await DbContext.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.Id == firstProduct.Id || p.Id == secondProduct.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        var firstPersisted = persistedProducts.Single(p => p.Id == firstProduct.Id);
        var firstResponseItem = responseContent.Items.FirstOrDefault(i => i.Id == firstProduct.Id);
        firstResponseItem.Should().NotBeNull();

        AssertionHelpers.AssertAllPropertiesAreMapped(
            sourceObject: firstPersisted,
            destinationObject: firstResponseItem,
            overridesMap: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(firstPersisted.CurrentSlug), nameof(firstResponseItem.Slug) }
            },
            sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(firstPersisted.SeoTitle),
                nameof(firstPersisted.FullDescription),
                nameof(firstPersisted.RowVersion),
                nameof(firstPersisted.Category),
                nameof(firstPersisted.Images)
            },
            equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        var secondPersisted = persistedProducts.Single(p => p.Id == secondProduct.Id);
        var secondResponseItem = responseContent.Items.FirstOrDefault(i => i.Id == secondProduct.Id);
        secondResponseItem.Should().NotBeNull();

        AssertionHelpers.AssertAllPropertiesAreMapped(
            sourceObject: secondPersisted,
            destinationObject: secondResponseItem,
            overridesMap: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(secondPersisted.CurrentSlug), nameof(secondResponseItem.Slug) }
            },
            sourceExcludes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(secondPersisted.SeoTitle),
                nameof(secondPersisted.FullDescription),
                nameof(secondPersisted.RowVersion),
                nameof(secondPersisted.Category),
                nameof(secondPersisted.Images)
            },
            equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));
    }

    [Fact]
    public async Task ListAdminProductsEndpoint_ShouldRespondWith400BadRequest_WhenOrderByParameterIsInvalid()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        var response = await client.GetAsync($"{EndpointUrl}?orderBy=InvalidColumn desc", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(ListAdminProductsRequest.OrderBy), ProductValidation.ErrorCodes.InvalidSortParameters);
    }
}
