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

namespace ForeverBloom.Application.IntegrationTests.Categories.Commands;

public sealed class CreateCategoryUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task CreateCategory_ShouldCreateRootCategory_WhenOnlyRequiredFieldsProvided()
    {
        var name = $"Minimal-{TestToken}";
        var slug = $"minimal-{TestToken}";

        var commandResult = CreateCategoryCommand.Create(
            name: name,
            slug: slug);
        commandResult.Should().BeSuccess();

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var categoryId = result.Value!.CategoryId;
        categoryId.Should().BeGreaterThan(0);

        var category = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == categoryId, ct));

        category.Should().NotBeNull();
        category.Name.Should().HaveValue(name);
        category.CurrentSlug.Should().HaveValue(slug);
        category.Path.Should().HaveValue(slug);
        category.Description.Should().BeNull();
        category.Image.Should().BeNull();
        category.ParentCategoryId.Should().BeNull();
        category.DisplayOrder.Should().Be(0);
        category.CreatedAt.Should().Be(actionTimestamp);
        category.UpdatedAt.Should().Be(actionTimestamp);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityId == categoryId && r.EntityType == EntityType.Category)
                .ToArrayAsync(ct));

        slugRegistrations.Should().ContainSingle();
        var slugRegistration = slugRegistrations.Single();
        slugRegistration.Slug.Should().HaveValue(slug);
        slugRegistration.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCategory_ShouldCreateRootCategory_WhenAllFieldsProvided()
    {
        var name = $"Roses-{TestToken}";
        var description = $"Seasonal roses collection no {TestToken}";
        var slug = $"roses-{TestToken}";
        var imageSource = $"/images/{TestToken}.jpg";
        var imageAltText = $"Image {TestToken}";
        const int displayOrder = 3;

        var commandResult = CreateCategoryCommand.Create(
            name: name,
            slug: slug,
            description: description,
            imagePath: imageSource,
            imageAltText: imageAltText,
            parentCategoryId: null,
            displayOrder: displayOrder);
        commandResult.Should().BeSuccess();

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var categoryId = result.Value!.CategoryId;
        categoryId.Should().BeGreaterThan(0);

        var category = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == categoryId, ct));

        category.Should().NotBeNull();
        category.Name.Should().HaveValue(name);
        category.CurrentSlug.Should().HaveValue(slug);
        category.Path.Should().HaveValue(slug);
        category.Description.Should().HaveValue(description);
        category.Image.Should().Match(imageSource, imageAltText);
        category.ParentCategoryId.Should().BeNull();
        category.DisplayOrder.Should().Be(displayOrder);
        category.CreatedAt.Should().Be(actionTimestamp);
        category.UpdatedAt.Should().Be(actionTimestamp);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityId == categoryId && r.EntityType == EntityType.Category)
                .ToArrayAsync(ct));

        slugRegistrations.Should().ContainSingle();
        var slugRegistration = slugRegistrations.Single();
        slugRegistration.Slug.Should().HaveValue(slug);
        slugRegistration.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCategory_ShouldCreateChildCategory_WhenParentExists()
    {
        var parentCategory = await Fixture.GivenCategoryAsync(
            name: $"Parent-{TestToken}",
            slug: $"parent-{TestToken}");

        var childName = $"Child-{TestToken}";
        var childSlug = $"child-{TestToken}";
        var childDescription = $"Child roses collection no {TestToken}";
        var childImageSource = $"/images/{TestToken}-child.jpg";
        var childImageAltText = $"Child image {TestToken}";
        const int displayOrder = 5;

        var commandResult = CreateCategoryCommand.Create(
            name: childName,
            slug: childSlug,
            description: childDescription,
            imagePath: childImageSource,
            imageAltText: childImageAltText,
            parentCategoryId: parentCategory.Id,
            displayOrder: displayOrder);
        commandResult.Should().BeSuccess();

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var childCategoryId = result.Value!.CategoryId;
        childCategoryId.Should().BeGreaterThan(0);

        var childCategory = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == childCategoryId, ct));

        childCategory.Should().NotBeNull();
        childCategory.Name.Should().HaveValue(childName);
        childCategory.Description.Should().HaveValue(childDescription);
        childCategory.CurrentSlug.Should().HaveValue(childSlug);
        childCategory.Image.Should().Match(childImageSource, childImageAltText);
        childCategory.ParentCategoryId.Should().Be(parentCategory.Id);
        childCategory.DisplayOrder.Should().Be(displayOrder);
        childCategory.Path.Should().HaveValue($"{parentCategory.Path.Value}.{childSlug}");
        childCategory.CreatedAt.Should().Be(actionTimestamp);
        childCategory.UpdatedAt.Should().Be(actionTimestamp);

        var childSlugRegistration = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.EntityType == EntityType.Category && r.EntityId == childCategoryId,
                    ct));

        childSlugRegistration.Should().NotBeNull();
        childSlugRegistration.Slug.Should().HaveValue(childSlug);
        childSlugRegistration.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCategory_ShouldAllowSameNameUnderDifferentParents()
    {
        var sharedName = $"Flowers-{TestToken}";

        var parent1 = await Fixture.GivenCategoryAsync(
            name: $"Parent1-{TestToken}");

        var parent2 = await Fixture.GivenCategoryAsync(
            name: $"Parent2-{TestToken}");

        await Fixture.GivenCategoryAsync(
            name: sharedName,
            parentCategoryId: parent1.Id);

        var commandResult = CreateCategoryCommand.Create(
            name: sharedName,
            slug: $"flowers-p2-{TestToken}",
            description: $"Flowers under parent 2 no {TestToken}",
            imagePath: $"/images/{TestToken}-p2.jpg",
            imageAltText: $"Flowers P2 {TestToken}",
            parentCategoryId: parent2.Id,
            displayOrder: 2);
        commandResult.Should().BeSuccess();

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var child2Id = result.Value!.CategoryId;

        var child2 = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == child2Id, ct));

        child2.Should().NotBeNull();
        child2.Name.Should().HaveValue(sharedName);
        child2.ParentCategoryId.Should().Be(parent2.Id);
        child2.CreatedAt.Should().Be(actionTimestamp);
        child2.UpdatedAt.Should().Be(actionTimestamp);
    }

    [Fact]
    public async Task CreateCategory_ShouldFail_WhenParentCategoryDoesNotExist()
    {
        const long nonExistentParentId = 999999;

        var commandResult = CreateCategoryCommand.Create(
            name: $"Orphan-{TestToken}",
            slug: $"orphan-{TestToken}",
            description: $"Orphan description no {TestToken}",
            imagePath: $"/images/{TestToken}-orphan.jpg",
            imageAltText: $"Orphan image {TestToken}",
            parentCategoryId: nonExistentParentId,
            displayOrder: 2);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.ParentNotFound>();
        error.ParentCategoryId.Should().Be(nonExistentParentId);

        var categories = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .ToListAsync(ct));
        categories.Should().BeEmpty();

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .ToListAsync(ct));
        slugRegistrations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCategory_ShouldFail_WhenSlugAlreadyExists()
    {
        var duplicateSlug = $"shared-{TestToken}";

        await Fixture.GivenCategoryAsync(
            name: $"Existing-{TestToken}",
            slug: duplicateSlug);

        var commandResult = CreateCategoryCommand.Create(
            name: $"Another-{TestToken}",
            slug: duplicateSlug,
            description: $"Another description no {TestToken}",
            imagePath: $"/images/{TestToken}-another.jpg",
            imageAltText: $"Another image {TestToken}",
            parentCategoryId: null,
            displayOrder: 4);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.SlugNotAvailable>();
        error.Slug.Should().Be(duplicateSlug);

        var categories = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .ToListAsync(ct));
        categories.Should().HaveCount(1);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .ToListAsync(ct));
        slugRegistrations.Should().HaveCount(1);
        var slugRegistration = slugRegistrations.Single();
        slugRegistration.Slug.Should().HaveValue(duplicateSlug);
        slugRegistration.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCategory_ShouldFail_WhenNameDuplicateWithinSameParent()
    {
        var duplicateName = $"Roses-{TestToken}";
        var existingSlug = $"roses-{TestToken}";

        await Fixture.GivenCategoryAsync(
            name: duplicateName,
            slug: existingSlug);

        var newSlug = $"unique-{TestToken}";

        var commandResult = CreateCategoryCommand.Create(
            name: duplicateName,
            slug: newSlug,
            description: $"Duplicate description no {TestToken}",
            imagePath: $"/images/{TestToken}-duplicate.jpg",
            imageAltText: $"Duplicate image {TestToken}",
            parentCategoryId: null,
            displayOrder: 6);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NameNotUniqueWithinParent>();
        error.Name.Should().Be(duplicateName);
        error.ParentCategoryId.Should().BeNull();

        var categories = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .ToListAsync(ct));
        categories.Should().HaveCount(1);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .ToListAsync(ct));
        slugRegistrations.Should().HaveCount(1);
        slugRegistrations.Single().Slug.Should().HaveValue(existingSlug);
    }
}
