using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ArchiveCategory;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Validation;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.ArchiveCategory;

public sealed class ArchiveCategoryEndpointTests : BackendAppTestClassBase
{
    private static string EndpointUrl(int categoryId) => $"/api/v1/admin/categories/{categoryId}/archive";

    public ArchiveCategoryEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task ArchiveCategoryEndpoint_ShouldRespondWith200Ok_WhenArchiveIsSuccessful()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Archive-{_testId:N}"[..20],
          slug: $"archive-{_testId:N}"[..20],
          description: "Category to be archived",
          imagePath: "/images/archive.jpg",
          displayOrder: 5,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        // Act: Archive the category
        var lowerBound = TimeProvider.System.GetUtcNow();
        var archiveRequest = new ArchiveCategoryRequest
        {
            RowVersion = createdCategory.RowVersion
        };
        var response = await client.PostAsJsonAsync(EndpointUrl(createdCategory.Id), archiveRequest, TestContext.Current.CancellationToken);
        var upperBound = TimeProvider.System.GetUtcNow();

        // Assert response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<ArchiveCategoryResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        // Assert database state
        var persistedCategory = await DbContext.Categories.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(c => c.Id == createdCategory.Id, TestContext.Current.CancellationToken);
        persistedCategory.Should().NotBeNull();
        persistedCategory.DeletedAt.Should().BeOnOrAfter(lowerBound);
        persistedCategory.DeletedAt.Should().BeOnOrBefore(upperBound);
        persistedCategory.RowVersion.Should().BeGreaterThan(createdCategory.RowVersion);

        // Verify response content
        responseContent.DeletedAt.Should().BeCloseTo(persistedCategory.DeletedAt.Value, TemporalTolerances.DatabaseTimestamp);
        responseContent.RowVersion.Should().Be(persistedCategory.RowVersion);

        // Verify category is soft-deleted (not accessible via normal queries)
        var publicCategory = await DbContext.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == createdCategory.Id, TestContext.Current.CancellationToken);
        publicCategory.Should().BeNull();
    }

    [Fact]
    public async Task ArchiveCategoryEndpoint_ShouldRespondWith400BadRequest_WhenCategoryHasChildren()
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

        // Act: Try to archive parent category (should fail due to children)
        var archiveRequest = new ArchiveCategoryRequest
        {
            RowVersion = parentCategory.RowVersion
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(parentCategory.Id), archiveRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty("CategoryId", CategoryValidation.ErrorCodes.CannotArchiveCategoryWithChildren);
    }

    [Fact]
    public async Task ArchiveCategoryEndpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Act: Try to archive non-existent category
        var archiveRequest = new ArchiveCategoryRequest
        {
            RowVersion = 1
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(999999), archiveRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ArchiveCategoryEndpoint_ShouldRespondWith409Conflict_WhenConcurrentUpdateOccurs()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Conflict-{_testId:N}"[..20],
          slug: $"conflict-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Simulate concurrent update by modifying the category directly in the database
        var category = await DbContext.Categories.SingleAsync(c => c.Id == createdCategory.Id, TestContext.Current.CancellationToken);
        category.Name = "Concurrently Modified";
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: Try to archive with stale row version
        var archiveRequest = new ArchiveCategoryRequest
        {
            RowVersion = createdCategory.RowVersion // This is now stale
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(createdCategory.Id), archiveRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
