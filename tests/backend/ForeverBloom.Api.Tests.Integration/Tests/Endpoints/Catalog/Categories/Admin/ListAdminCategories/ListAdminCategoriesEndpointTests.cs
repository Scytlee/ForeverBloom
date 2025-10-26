using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ListAdminCategories;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Extensions;
using ForeverBloom.Testing.Common.Helpers.Validation;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.ListAdminCategories;

public sealed class ListAdminCategoriesEndpointTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/admin/categories";

    public ListAdminCategoriesEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task ListAdminCategoriesEndpoint_ShouldRespondWith400BadRequest_WhenOrderByParameterIsInvalid()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        var response = await client.GetAsync($"{EndpointUrl}?orderBy=InvalidColumn desc", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(ListAdminCategoriesRequest.OrderBy), CategoryValidation.ErrorCodes.InvalidSortParameters);
    }

    [Fact]
    public async Task ListAdminCategoriesEndpoint_ShouldRespondWith200Ok_AndPaginatedCategories_WhenRequestIsValid()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        var firstCategory = await client.CreateCategoryAsync(
          name: $"Category-{_testId:N}"[..20],
          slug: $"cat-{_testId:N}"[..20],
          displayOrder: 2,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var secondCategory = await client.CreateCategoryAsync(
          name: $"AnotherCat-{_testId:N}"[..20],
          slug: $"another-{_testId:N}"[..20],
          displayOrder: 1,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"{EndpointUrl}?pageNumber=1&pageSize=10", TestContext.Current.CancellationToken);

        // Verify response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<ListAdminCategoriesResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        // Verify paging metadata
        responseContent.PageNumber.Should().Be(1);
        responseContent.PageSize.Should().Be(10);
        responseContent.TotalCount.Should().BeGreaterOrEqualTo(2);
        responseContent.Items.Should().HaveCountGreaterOrEqualTo(2);

        // Verify database state mapping
        var persistedCategories = await DbContext.Categories.AsNoTracking()
          .Where(c => c.Id == firstCategory.Id || c.Id == secondCategory.Id)
          .ToListAsync(TestContext.Current.CancellationToken);

        var firstPersisted = persistedCategories.Single(c => c.Id == firstCategory.Id);
        var firstResponseItem = responseContent.Items.FirstOrDefault(i => i.Id == firstCategory.Id);
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
        nameof(firstPersisted.Path),
        nameof(firstPersisted.ParentCategory),
        nameof(firstPersisted.ChildCategories),
        nameof(firstPersisted.Products)
          },
          equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));

        var secondPersisted = persistedCategories.Single(c => c.Id == secondCategory.Id);
        var secondResponseItem = responseContent.Items.FirstOrDefault(i => i.Id == secondCategory.Id);
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
        nameof(secondPersisted.Path),
        nameof(secondPersisted.ParentCategory),
        nameof(secondPersisted.ChildCategories),
        nameof(secondPersisted.Products)
          },
          equivalencyConfig: options => options.WithTimestampTolerance(TemporalTolerances.DatabaseTimestamp));
    }
}
