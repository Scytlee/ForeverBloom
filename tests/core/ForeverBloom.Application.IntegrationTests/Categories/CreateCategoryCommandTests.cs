using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Categories.Commands.CreateCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.SlugRegistry;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using FluentAssertions;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;
using Microsoft.EntityFrameworkCore;
using CategoryErrors = ForeverBloom.Application.Categories.CategoryErrors;

namespace ForeverBloom.Application.IntegrationTests.Categories;

public sealed class CreateCategoryCommandTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task CreateCategory_ShouldCorrectlyCreateRootCategory_OnValidCommand()
    {
        var testToken = TestId.ToString("N");
        var cancellationToken = TestContext.Current.CancellationToken;

        var name = $"Roses-{testToken}";
        var description = $"Seasonal roses collection no {testToken}";
        var slug = $"roses-{testToken}";
        var imageSource = $"/images/{testToken}.jpg";
        var imageAltText = $"Image {testToken}";
        const int displayOrder = 3;

        var command = new CreateCategoryCommand(
            Name: name,
            Description: description,
            Slug: slug,
            ImagePath: imageSource,
            ImageAltText: imageAltText,
            ParentCategoryId: null,
            DisplayOrder: displayOrder);

        var result = await SendAsync(command, cancellationToken);

        result.Should().BeSuccess();
        var categoryId = result.Value!.CategoryId;
        categoryId.Should().BeGreaterThan(0);

        var category = await DbContext.Set<Category>()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        category.Should().NotBeNull();
        category.Name.Should().HaveValue(name);
        category.Description.Should().HaveValue(description);
        category.CurrentSlug.Should().HaveValue(slug);
        category.Image.Should().Match(imageSource, imageAltText);
        category.ParentCategoryId.Should().BeNull();
        category.DisplayOrder.Should().Be(displayOrder);
        category.Path.Should().HaveValue(slug);

        var slugRegistrations = await DbContext.Set<SlugRegistration>()
            .AsNoTracking()
            .Where(r => r.EntityId == categoryId && r.EntityType == EntityType.Category)
            .ToListAsync(cancellationToken);

        slugRegistrations.Should().ContainSingle();
        var slugRegistration = slugRegistrations.Single();
        slugRegistration.Slug.Should().HaveValue(slug);
        slugRegistration.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCategory_ShouldCorrectlyCreateChildCategory_WhenParentExists()
    {
        var testToken = TestId.ToString("N");
        var cancellationToken = TestContext.Current.CancellationToken;

        var parentResult = await Fixture.GivenCategoryAsync(
            name: $"Parent-{testToken}",
            slug: $"parent-{testToken}",
            cancellationToken: cancellationToken);

        var parentCategory = await DbContext.Set<Category>()
            .AsNoTracking()
            .SingleAsync(c => c.Id == parentResult.CategoryId, cancellationToken);

        var childName = $"Child-{testToken}";
        var childSlug = $"child-{testToken}";
        var childDescription = $"Child roses collection no {testToken}";
        var childImageSource = $"/images/{testToken}-child.jpg";
        var childImageAltText = $"Child image {testToken}";
        const int displayOrder = 5;

        var command = new CreateCategoryCommand(
            Name: childName,
            Description: childDescription,
            Slug: childSlug,
            ImagePath: childImageSource,
            ImageAltText: childImageAltText,
            ParentCategoryId: parentResult.CategoryId,
            DisplayOrder: displayOrder);

        var result = await SendAsync(command, cancellationToken);

        result.Should().BeSuccess();
        var childCategoryId = result.Value!.CategoryId;
        childCategoryId.Should().BeGreaterThan(0);

        var childCategory = await DbContext.Set<Category>()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == childCategoryId, cancellationToken);

        childCategory.Should().NotBeNull();
        childCategory.Name.Should().HaveValue(childName);
        childCategory.Description.Should().HaveValue(childDescription);
        childCategory.CurrentSlug.Should().HaveValue(childSlug);
        childCategory.Image.Should().Match(childImageSource, childImageAltText);
        childCategory.ParentCategoryId.Should().Be(parentResult.CategoryId);
        childCategory.DisplayOrder.Should().Be(displayOrder);
        childCategory.Path.Should().HaveValue($"{parentCategory.Path.Value}.{childSlug}");

        var childSlugRegistration = await DbContext.Set<SlugRegistration>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.EntityType == EntityType.Category && r.EntityId == childCategoryId,
                cancellationToken);

        childSlugRegistration.Should().NotBeNull();
        childSlugRegistration.Slug.Should().HaveValue(childSlug);
        childSlugRegistration.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCategory_ShouldFail_WhenSlugAlreadyExists()
    {
        var testToken = TestId.ToString("N");
        var cancellationToken = TestContext.Current.CancellationToken;

        var duplicateSlug = $"shared-{testToken}";

        await Fixture.GivenCategoryAsync(
            name: $"Existing-{testToken}",
            slug: duplicateSlug,
            cancellationToken: cancellationToken);

        var command = new CreateCategoryCommand(
            Name: $"Another-{testToken}",
            Description: $"Another description no {testToken}",
            Slug: duplicateSlug,
            ImagePath: $"/images/{testToken}-another.jpg",
            ImageAltText: $"Another image {testToken}",
            ParentCategoryId: null,
            DisplayOrder: 4);

        var result = await SendAsync(command, cancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.SlugNotAvailable>();
        error.Slug.Should().Be(duplicateSlug);

        var categories = await DbContext.Set<Category>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        categories.Should().HaveCount(1);

        var slugRegistrations = await DbContext.Set<SlugRegistration>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        slugRegistrations.Should().HaveCount(1);
        var slugRegistration = slugRegistrations.Single();
        slugRegistration.Slug.Should().HaveValue(duplicateSlug);
        slugRegistration.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCategory_ShouldFail_WhenNameDuplicateWithinSameParent()
    {
        var testToken = TestId.ToString("N");
        var cancellationToken = TestContext.Current.CancellationToken;

        var duplicateName = $"Roses-{testToken}";
        var existingSlug = $"roses-{testToken}";

        await Fixture.GivenCategoryAsync(
            name: duplicateName,
            slug: existingSlug,
            cancellationToken: cancellationToken);

        var newSlug = $"unique-{testToken}";

        var command = new CreateCategoryCommand(
            Name: duplicateName,
            Description: $"Duplicate description no {testToken}",
            Slug: newSlug,
            ImagePath: $"/images/{testToken}-duplicate.jpg",
            ImageAltText: $"Duplicate image {testToken}",
            ParentCategoryId: null,
            DisplayOrder: 6);

        var result = await SendAsync(command, cancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NameNotUniqueWithinParent>();
        error.Name.Should().Be(duplicateName);
        error.ParentCategoryId.Should().BeNull();

        var categories = await DbContext.Set<Category>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        categories.Should().HaveCount(1);

        var slugRegistrations = await DbContext.Set<SlugRegistration>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        slugRegistrations.Should().HaveCount(1);
        slugRegistrations.Single().Slug.Should().HaveValue(existingSlug);
    }

    [Fact]
    public async Task CreateCategory_ShouldFail_WhenParentCategoryDoesNotExist()
    {
        var testToken = TestId.ToString("N");
        var cancellationToken = TestContext.Current.CancellationToken;
        const long nonExistentParentId = 999999;

        var command = new CreateCategoryCommand(
            Name: $"Orphan-{testToken}",
            Description: $"Orphan description no {testToken}",
            Slug: $"orphan-{testToken}",
            ImagePath: $"/images/{testToken}-orphan.jpg",
            ImageAltText: $"Orphan image {testToken}",
            ParentCategoryId: nonExistentParentId,
            DisplayOrder: 2);

        var result = await SendAsync(command, cancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.ParentNotFound>();
        error.ParentCategoryId.Should().Be(nonExistentParentId);

        var categories = await DbContext.Set<Category>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        categories.Should().BeEmpty();

        var slugRegistrations = await DbContext.Set<SlugRegistration>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        slugRegistrations.Should().BeEmpty();
    }
}
