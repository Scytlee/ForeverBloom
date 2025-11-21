using ForeverBloom.Application.Categories.Queries.GetCategoryById;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;

namespace ForeverBloom.Application.IntegrationTests.Categories.Queries;

public sealed class GetCategoryByIdUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task GetCategoryById_ShouldReturnRootCategory_WhenOnlyRequiredFieldsProvided()
    {
        var name = $"Minimal-{TestToken}";
        var slug = $"minimal-{TestToken}";

        var creationTimestamp = Fixture.TimeProvider.CurrentTime;
        var category = await Fixture.GivenCategoryAsync(
            name: name,
            slug: slug,
            creationTimestamp: creationTimestamp);

        var queryResult = GetCategoryByIdQuery.Create(category.Id);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Id.Should().Be(category.Id);
        payload.Name.Should().Be(name);
        payload.Description.Should().BeNull();
        payload.Slug.Should().Be(slug);
        payload.ImagePath.Should().BeNull();
        payload.ImageAltText.Should().BeNull();
        payload.ParentCategoryId.Should().BeNull();
        payload.DisplayOrder.Should().Be(0);
        payload.PublishStatusCode.Should().Be(PublishStatus.Draft.Code);
        payload.Path.Should().Be(slug);
        payload.CreatedAt.Should().Be(creationTimestamp);
        payload.UpdatedAt.Should().Be(creationTimestamp);
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturnChildCategoryWithFullPayload()
    {
        var parentName = $"Parent-{TestToken}";
        var parentSlug = $"parent-{TestToken}";
        var parent = await Fixture.GivenCategoryAsync(
            name: parentName,
            slug: parentSlug,
            description: $"Parent description {TestToken}",
            imagePath: $"/images/{TestToken}-parent.jpg",
            imageAltText: $"Parent image {TestToken}",
            displayOrder: 2);

        var childName = $"Child-{TestToken}";
        var childDescription = $"Child description {TestToken}";
        var childSlug = $"child-{TestToken}";
        var childImagePath = $"/images/{TestToken}-child.jpg";
        var childImageAltText = $"Child image {TestToken}";
        const int childDisplayOrder = 7;

        var creationTimestamp = Fixture.TimeProvider.CurrentTime;
        var child = await Fixture.GivenCategoryAsync(
            name: childName,
            description: childDescription,
            slug: childSlug,
            imagePath: childImagePath,
            imageAltText: childImageAltText,
            parentCategoryId: parent.Id,
            displayOrder: childDisplayOrder,
            publishStatus: PublishStatus.Published,
            creationTimestamp: creationTimestamp);

        var queryResult = GetCategoryByIdQuery.Create(child.Id);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Id.Should().Be(child.Id);
        payload.Name.Should().Be(childName);
        payload.Description.Should().Be(childDescription);
        payload.Slug.Should().Be(childSlug);
        payload.ImagePath.Should().Be(childImagePath);
        payload.ImageAltText.Should().Be(childImageAltText);
        payload.ParentCategoryId.Should().Be(parent.Id);
        payload.DisplayOrder.Should().Be(childDisplayOrder);
        payload.PublishStatusCode.Should().Be(PublishStatus.Published.Code);
        payload.Path.Should().Be($"{parentSlug}.{childSlug}");
        payload.CreatedAt.Should().Be(creationTimestamp);
        payload.UpdatedAt.Should().Be(creationTimestamp);
        payload.DeletedAt.Should().BeNull();
        payload.RowVersion.Should().Be(child.RowVersion);
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        const long missingCategoryId = 999_999L;

        var queryResult = GetCategoryByIdQuery.Create(missingCategoryId);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NotFoundById>();
        error.Id.Should().Be(missingCategoryId);
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturnFailure_WhenCategoryIsArchived()
    {
        var category = await Fixture.GivenCategoryAsync(
            name: $"Archived-{TestToken}",
            slug: $"archived-{TestToken}",
            isArchived: true);

        var queryResult = GetCategoryByIdQuery.Create(category.Id);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NotFoundById>();
        error.Id.Should().Be(category.Id);
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturnFailure_WhenCategoryHasArchivedAncestor()
    {
        var parent = await Fixture.GivenCategoryAsync(
            name: $"Parent-{TestToken}",
            slug: $"parent-{TestToken}");

        var child = await Fixture.GivenCategoryAsync(
            name: $"Child-{TestToken}",
            slug: $"child-{TestToken}",
            parentCategoryId: parent.Id);

        await Fixture.ArchiveExistingCategoryAsync(parent);

        var queryResult = GetCategoryByIdQuery.Create(child.Id);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NotFoundById>();
        error.Id.Should().Be(child.Id);
    }
}
