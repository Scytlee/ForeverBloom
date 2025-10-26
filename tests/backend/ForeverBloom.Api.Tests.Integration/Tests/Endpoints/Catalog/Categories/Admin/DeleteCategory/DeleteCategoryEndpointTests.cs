using System.Net;
using FluentAssertions;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Api.Tests.Helpers.Arrange;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Validation;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.Admin.DeleteCategory;

public sealed class DeleteCategoryEndpointTests : BackendAppTestClassBase
{
    public DeleteCategoryEndpointTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    private static string GetEndpointUrl(int categoryId, uint? rowVersion = null)
    {
        return $"/api/v1/admin/categories/{categoryId}{(rowVersion.HasValue ? $"?RowVersion={rowVersion.Value}" : "")}";
    }

    [Fact]
    public async Task DeleteCategoryEndpoint_ShouldRespondWith204NoContent_WhenRequestIsValid()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create and archive a category
        var category = await client.CreateCategoryAsync(
            name: $"Test Category {_testId:N}"[..20],
            slug: $"test-cat-{_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);
        var archivedCategory = await client.ArchiveCategoryAsync(category.Id, category.RowVersion, TestContext.Current.CancellationToken);

        // Act: Delete the archived category
        var response = await client.DeleteAsync(GetEndpointUrl(category.Id, archivedCategory.RowVersion), TestContext.Current.CancellationToken);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert database state: category should be hard deleted
        var deletedCategory = await DbContext.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);
        deletedCategory.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategoryEndpoint_ShouldRespondWith400BadRequest_WhenCategoryNotArchived()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create a category but don't archive it
        var category = await client.CreateCategoryAsync(
            name: $"Test Category {_testId:N}"[..20],
            slug: $"test-cat-{_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);

        // Act: Attempt to delete the non-archived category
        var response = await client.DeleteAsync(GetEndpointUrl(category.Id, category.RowVersion), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty("categoryId", CategoryValidation.ErrorCodes.CategoryNotArchived);

        // Verify category still exists in database
        var existingCategory = await DbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);
        existingCategory.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCategoryEndpoint_ShouldRespondWith404NotFound_WhenCategoryDoesNotExist()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Act: Attempt to delete a non-existent category
        const int nonExistentId = 999999;
        var response = await client.DeleteAsync(GetEndpointUrl(nonExistentId, 1), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategoryEndpoint_ShouldRespondWith409Conflict_WhenConcurrencyConflictOccurs()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();

        // Arrange: Create and archive a category
        var category = await client.CreateCategoryAsync(
            name: $"Test Category {_testId:N}"[..20],
            slug: $"test-cat-{_testId:N}"[..20],
            cancellationToken: TestContext.Current.CancellationToken);
        var archivedCategory = await client.ArchiveCategoryAsync(category.Id, category.RowVersion, TestContext.Current.CancellationToken);

        // Simulate concurrent modification by updating the category
        // Update endpoint does not handle archived entities as of now, so it must be updated via database
        var categoryInDb = await DbContext.Categories
            .IgnoreQueryFilters()
            .SingleAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);
        categoryInDb.Name = "Modified Name";
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbContext.ChangeTracker.Clear();

        // Act: Attempt to delete with stale row version
        var response = await client.DeleteAsync(GetEndpointUrl(category.Id, archivedCategory.RowVersion), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Verify category still exists in database
        var existingCategory = await DbContext.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);
        existingCategory.Should().NotBeNull();
    }
}
