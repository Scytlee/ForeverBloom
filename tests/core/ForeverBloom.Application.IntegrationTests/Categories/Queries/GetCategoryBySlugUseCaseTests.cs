using ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using FluentAssertions;

namespace ForeverBloom.Application.IntegrationTests.Categories.Queries;

public sealed class GetCategoryBySlugUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task GetCategoryBySlug_ShouldReturnMinimalRootCategory_WhenOnlyRequiredFieldsProvided()
    {
        var name = $"Minimal-{TestToken}";
        var slug = $"minimal-{TestToken}";

        var category = await Fixture.GivenCategoryAsync(
            name: name,
            slug: slug,
            publishStatus: PublishStatus.Published);

        var queryResult = GetCategoryBySlugQuery.Create(slug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Id.Should().Be(category.Id);
        payload.Name.Should().Be(name);
        payload.Description.Should().BeNull();
        payload.Slug.Should().Be(slug);
        payload.ImageSource.Should().BeNull();
        payload.ImageAltText.Should().BeNull();
        payload.ParentCategoryId.Should().BeNull();
        payload.Breadcrumbs.Should().BeEquivalentTo([
            new BreadcrumbItem { Name = name, Slug = slug }
        ]);
    }

    [Fact]
    public async Task GetCategoryBySlug_ShouldReturnPublishedCategoryWithBreadcrumbs()
    {
        var rootName = $"Root-{TestToken}";
        var rootSlug = $"root-{TestToken}";
        var root = await Fixture.GivenCategoryAsync(
            name: rootName,
            slug: rootSlug,
            publishStatus: PublishStatus.Published);

        var parentName = $"Parent-{TestToken}";
        var parentSlug = $"parent-{TestToken}";
        var parent = await Fixture.GivenCategoryAsync(
            name: parentName,
            slug: parentSlug,
            parentCategoryId: root.Id,
            publishStatus: PublishStatus.Published);

        var childName = $"Leaf-{TestToken}";
        var childSlug = $"leaf-{TestToken}";
        var childDescription = $"Leaf description {TestToken}";
        var childImageSource = $"/images/{TestToken}-leaf.jpg";
        var childImageAltText = $"Leaf alt {TestToken}";
        var child = await Fixture.GivenCategoryAsync(
            name: childName,
            description: childDescription,
            slug: childSlug,
            imagePath: childImageSource,
            imageAltText: childImageAltText,
            parentCategoryId: parent.Id,
            publishStatus: PublishStatus.Published);

        var queryResult = GetCategoryBySlugQuery.Create(childSlug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;

        payload.Id.Should().Be(child.Id);
        payload.Name.Should().Be(childName);
        payload.Description.Should().Be(childDescription);
        payload.Slug.Should().Be(childSlug);
        payload.ImageSource.Should().Be(childImageSource);
        payload.ImageAltText.Should().Be(childImageAltText);
        payload.ParentCategoryId.Should().Be(parent.Id);
        payload.Breadcrumbs.Should().BeEquivalentTo([
            new BreadcrumbItem { Name = rootName, Slug = rootSlug },
            new BreadcrumbItem { Name = parentName, Slug = parentSlug },
            new BreadcrumbItem { Name = childName, Slug = childSlug }
        ]);
    }

    [Fact]
    public async Task GetCategoryBySlug_ShouldReturnFailure_WhenSlugDoesNotExist()
    {
        var missingSlug = $"missing-{TestToken}";

        var queryResult = GetCategoryBySlugQuery.Create(missingSlug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NotFoundBySlug>();
        error.Slug.Should().Be(missingSlug);
    }

    [Fact]
    public async Task GetCategoryBySlug_ShouldReturnSlugChanged_WhenHistoricalSlugProvided()
    {
        var originalSlug = $"garden-{TestToken}";
        var newSlug = $"garden-{TestToken}-v2";
        await Fixture.GivenCategoryAsync(
            slug: newSlug,
            publishStatus: PublishStatus.Published,
            slugHistory: [originalSlug]);

        var queryResult = GetCategoryBySlugQuery.Create(originalSlug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.SlugChanged>();
        error.AttemptedSlug.Should().Be(originalSlug);
        error.CurrentSlug.Should().Be(newSlug);
    }

    [Fact]
    public async Task GetCategoryBySlug_ShouldReturnFailure_WhenCategoryIsNotPublished()
    {
        var slug = $"draft-{TestToken}";

        await Fixture.GivenCategoryAsync(
            slug: slug,
            publishStatus: PublishStatus.Draft);

        var queryResult = GetCategoryBySlugQuery.Create(slug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NotFoundBySlug>();
        error.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task GetCategoryBySlug_ShouldReturnFailure_WhenCategoryIsArchived()
    {
        var slug = $"archived-{TestToken}";

        await Fixture.GivenCategoryAsync(
            slug: slug,
            publishStatus: PublishStatus.Published,
            isArchived: true);

        var queryResult = GetCategoryBySlugQuery.Create(slug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NotFoundBySlug>();
        error.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task GetCategoryBySlug_ShouldReturnFailure_WhenCategoryHasArchivedAncestor()
    {
        var parentSlug = $"parent-{TestToken}";
        var parent = await Fixture.GivenCategoryAsync(
            slug: parentSlug,
            publishStatus: PublishStatus.Published);

        var childSlug = $"child-{TestToken}";
        await Fixture.GivenCategoryAsync(
            slug: childSlug,
            parentCategoryId: parent.Id,
            publishStatus: PublishStatus.Published);

        await Fixture.ArchiveExistingCategoryAsync(parent);

        var queryResult = GetCategoryBySlugQuery.Create(childSlug);
        queryResult.Should().BeSuccess();

        var result = await SendAsync(queryResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NotFoundBySlug>();
        error.Slug.Should().Be(childSlug);
    }
}
