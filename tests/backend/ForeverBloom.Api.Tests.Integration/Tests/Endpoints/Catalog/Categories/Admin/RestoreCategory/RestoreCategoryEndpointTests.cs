using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.RestoreCategory;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Validation;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.RestoreCategory;

public sealed class RestoreCategoryEndpointTests : BackendAppTestClassBase
{
    private static string EndpointUrl(int categoryId) => $"/api/v1/admin/categories/{categoryId}/restore";

    public RestoreCategoryEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task RestoreCategoryEndpoint_ShouldRespondWith200Ok_WhenRestoreIsSuccessful()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Restore-{_testId:N}"[..20],
          slug: $"restore-{_testId:N}"[..20],
          description: "Category to be restored",
          imagePath: "/images/restore.jpg",
          displayOrder: 3,
          isActive: true,
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Archive the category first
        var archivedCategory = await client.ArchiveCategoryAsync(createdCategory.Id, createdCategory.RowVersion, TestContext.Current.CancellationToken);

        // Act: Restore the category
        var restoreRequest = new RestoreCategoryRequest
        {
            RowVersion = archivedCategory.RowVersion
        };
        var response = await client.PostAsJsonAsync(EndpointUrl(createdCategory.Id), restoreRequest, TestContext.Current.CancellationToken);

        // Assert response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<RestoreCategoryResponse>(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNull();

        // Assert database state
        var persistedCategory = await DbContext.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == createdCategory.Id, TestContext.Current.CancellationToken);
        persistedCategory.Should().NotBeNull();
        persistedCategory.DeletedAt.Should().BeNull();
        persistedCategory.RowVersion.Should().BeGreaterThan(archivedCategory.RowVersion);

        // Verify response content
        responseContent.DeletedAt.Should().BeNull();
        responseContent.RowVersion.Should().Be(persistedCategory.RowVersion);

        // Verify category is accessible via normal queries (not soft-deleted)
        var publicCategory = await DbContext.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == createdCategory.Id, TestContext.Current.CancellationToken);
        publicCategory.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreCategoryEndpoint_ShouldRespondWith400BadRequest_WhenRowVersionIsMissing()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create and archive a category
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Validation-{_testId:N}"[..20],
          slug: $"validation-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);
        await client.ArchiveCategoryAsync(createdCategory.Id, createdCategory.RowVersion, TestContext.Current.CancellationToken);

        // Act: Try to restore with invalid RowVersion
        var restoreRequest = new RestoreCategoryRequest
        {
            RowVersion = 0
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(createdCategory.Id), restoreRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(RestoreCategoryRequest.RowVersion), CategoryValidation.ErrorCodes.RowVersionRequired);
    }

    [Fact]
    public async Task RestoreCategoryEndpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Act: Try to restore non-existent category
        var restoreRequest = new RestoreCategoryRequest
        {
            RowVersion = 1
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(999999), restoreRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreCategoryEndpoint_ShouldRespondWith409Conflict_WhenConcurrentUpdateOccurs()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category
        var createdCategory = await client.CreateCategoryAsync(
          name: $"Conflict-{_testId:N}"[..20],
          slug: $"conflict-{_testId:N}"[..20],
          cancellationToken: TestContext.Current.CancellationToken);

        // Arrange: Archive the category
        var archivedCategory = await client.ArchiveCategoryAsync(createdCategory.Id, createdCategory.RowVersion, TestContext.Current.CancellationToken);

        // Arrange: Simulate concurrent update by modifying the category directly in the database
        var category = await DbContext.Categories.IgnoreQueryFilters().SingleAsync(c => c.Id == createdCategory.Id, TestContext.Current.CancellationToken);
        category.Name = "Concurrently Modified";
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: Try to restore with correct row version but concurrent modification occurred
        var restoreRequest = new RestoreCategoryRequest
        {
            RowVersion = archivedCategory.RowVersion
        };

        var response = await client.PostAsJsonAsync(EndpointUrl(createdCategory.Id), restoreRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
