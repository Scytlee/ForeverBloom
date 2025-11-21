using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Categories.Commands.UpdateCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Categories.Commands;

public sealed class UpdateCategoryUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task UpdateCategory_ShouldUpdateAllMutableFields_WhenValidCommand()
    {
        var categoryBefore = await Fixture.GivenCategoryAsync(
            name: $"Original-{TestToken}",
            slug: $"original-{TestToken}",
            description: $"Original description {TestToken}",
            imagePath: $"/images/{TestToken}-old.jpg",
            imageAltText: $"Old image {TestToken}",
            displayOrder: 5,
            publishStatus: PublishStatus.Draft);

        var updatedName = $"Updated-{TestToken}";
        var updatedDescription = $"Updated description {TestToken}";
        var updatedImagePath = $"/images/{TestToken}-new.jpg";
        var updatedImageAltText = $"New image {TestToken}";
        const int updatedDisplayOrder = 10;

        var updateTimestamp = new DateTimeOffset(2025, 3, 15, 10, 30, 0, TimeSpan.Zero);

        var commandResult = UpdateCategoryCommand.Create(
            categoryId: categoryBefore.Id,
            rowVersion: categoryBefore.RowVersion,
            name: updatedName,
            description: updatedDescription,
            imagePath: updatedImagePath,
            imageAltText: updatedImageAltText,
            displayOrder: updatedDisplayOrder,
            publishStatus: "published");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, updateTimestamp, CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Name.Should().Be(updatedName);
        payload.Description.Should().Be(updatedDescription);
        payload.ImagePath.Should().Be(updatedImagePath);
        payload.ImageAltText.Should().Be(updatedImageAltText);
        payload.DisplayOrder.Should().Be(updatedDisplayOrder);
        payload.PublishStatus.Should().Be(PublishStatus.Published);
        payload.RowVersion.Should().BeGreaterThan(categoryBefore.RowVersion);
        payload.UpdatedAt.Should().Be(updateTimestamp);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryBefore.Id, ct));

        categoryAfter.Name.Should().HaveValue(updatedName);
        categoryAfter.Description.Should().HaveValue(updatedDescription);
        categoryAfter.Image.Should().Match(updatedImagePath, updatedImageAltText);
        categoryAfter.DisplayOrder.Should().Be(updatedDisplayOrder);
        categoryAfter.PublishStatus.Should().Be(PublishStatus.Published);
        categoryAfter.RowVersion.Should().Be(payload.RowVersion);
        categoryAfter.UpdatedAt.Should().Be(payload.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCategory_ShouldClearNullableFields_WhenNullProvided()
    {
        var categoryBefore = await Fixture.GivenCategoryAsync(
            name: $"Clearable-{TestToken}",
            slug: $"clearable-{TestToken}",
            description: $"Description {TestToken}",
            imagePath: $"/images/{TestToken}.jpg",
            imageAltText: $"Image {TestToken}",
            displayOrder: 3,
            publishStatus: PublishStatus.Draft);

        var commandResult = UpdateCategoryCommand.Create(
            categoryId: categoryBefore.Id,
            rowVersion: categoryBefore.RowVersion,
            description: (string?)null,
            imagePath: (string?)null,
            publishStatus: "hidden");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Description.Should().BeNull();
        payload.ImagePath.Should().BeNull();
        payload.ImageAltText.Should().BeNull();
        payload.PublishStatus.Should().Be(PublishStatus.Hidden);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryBefore.Id, ct));

        categoryAfter.Description.Should().BeNull();
        categoryAfter.Image.Should().BeNull();
        categoryAfter.PublishStatus.Should().Be(PublishStatus.Hidden);
    }

    [Fact]
    public async Task UpdateCategory_ShouldUpdateOnlySomeFields_WhenPartialUpdateProvided()
    {
        var categoryBefore = await Fixture.GivenCategoryAsync(
            name: $"Partial-{TestToken}",
            slug: $"partial-{TestToken}",
            description: $"Original description {TestToken}",
            imagePath: $"/images/{TestToken}.jpg",
            imageAltText: $"Original image {TestToken}",
            displayOrder: 2,
            publishStatus: PublishStatus.Draft);

        var newName = $"Updated-{TestToken}";
        const int newDisplayOrder = 7;

        var commandResult = UpdateCategoryCommand.Create(
            categoryId: categoryBefore.Id,
            rowVersion: categoryBefore.RowVersion,
            name: newName,
            displayOrder: newDisplayOrder);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Name.Should().Be(newName);
        payload.DisplayOrder.Should().Be(newDisplayOrder);
        payload.Description.Should().Be(categoryBefore.Description!.Value);
        payload.ImagePath.Should().Be(categoryBefore.Image!.Source.Value);
        payload.ImageAltText.Should().Be(categoryBefore.Image.AltText);
        payload.PublishStatus.Should().Be(categoryBefore.PublishStatus);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryBefore.Id, ct));

        categoryAfter.Name.Should().HaveValue(newName);
        categoryAfter.DisplayOrder.Should().Be(newDisplayOrder);
        categoryAfter.Description.Should().HaveValue(categoryBefore.Description!.Value);
        categoryAfter.Image.Should().Match(categoryBefore.Image!.Source.Value, categoryBefore.Image.AltText);
        categoryAfter.PublishStatus.Should().Be(categoryBefore.PublishStatus);
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturnSuccessWithoutPersisting_WhenNoFieldsChange()
    {
        var categoryBefore = await Fixture.GivenCategoryAsync(
            name: $"NoChange-{TestToken}",
            slug: $"no-change-{TestToken}",
            displayOrder: 1,
            publishStatus: PublishStatus.Draft);

        var commandResult = UpdateCategoryCommand.Create(
            categoryId: categoryBefore.Id,
            rowVersion: categoryBefore.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.RowVersion.Should().Be(categoryBefore.RowVersion);
        payload.UpdatedAt.Should().Be(categoryBefore.UpdatedAt);
        payload.Name.Should().Be(categoryBefore.Name.Value);
        payload.DisplayOrder.Should().Be(categoryBefore.DisplayOrder);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryBefore.Id, ct));

        categoryAfter.RowVersion.Should().Be(categoryBefore.RowVersion);
        categoryAfter.UpdatedAt.Should().Be(categoryBefore.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCategory_ShouldAllowValidPublishStatusTransition()
    {
        var categoryBefore = await Fixture.GivenCategoryAsync(
            name: $"Draft-{TestToken}",
            slug: $"draft-{TestToken}",
            publishStatus: PublishStatus.Draft);

        var commandResult = UpdateCategoryCommand.Create(
            categoryId: categoryBefore.Id,
            rowVersion: categoryBefore.RowVersion,
            publishStatus: "published");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.PublishStatus.Should().Be(PublishStatus.Published);
        payload.RowVersion.Should().BeGreaterThan(categoryBefore.RowVersion);

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryBefore.Id, ct));

        categoryAfter.PublishStatus.Should().Be(PublishStatus.Published);
        categoryAfter.RowVersion.Should().Be(payload.RowVersion);
    }

    [Fact]
    public async Task UpdateCategory_ShouldFail_WhenCategoryDoesNotExist()
    {
        const long nonExistentCategoryId = 999_999_999;

        var commandResult = UpdateCategoryCommand.Create(
            categoryId: nonExistentCategoryId,
            rowVersion: 1,
            name: $"DoesNotMatter-{TestToken}");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.NotFoundById>();
        error.Id.Should().Be(nonExistentCategoryId);
    }

    [Fact]
    public async Task UpdateCategory_ShouldFail_WhenRowVersionMismatch()
    {
        var categoryBefore = await Fixture.GivenCategoryAsync(
            name: $"Stale-{TestToken}",
            slug: $"stale-{TestToken}");

        var staleRowVersion = categoryBefore.RowVersion + 1;

        var commandResult = UpdateCategoryCommand.Create(
            categoryId: categoryBefore.Id,
            rowVersion: staleRowVersion,
            name: $"StaleUpdate-{TestToken}");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryBefore.Id, ct));

        categoryAfter.RowVersion.Should().Be(categoryBefore.RowVersion);
        categoryAfter.Name.Should().HaveValue(categoryBefore.Name.Value);
    }

    [Fact]
    public async Task UpdateCategory_ShouldFail_WhenPublishStatusTransitionNotAllowed()
    {
        var categoryBefore = await Fixture.GivenCategoryAsync(
            name: $"Published-{TestToken}",
            slug: $"published-{TestToken}",
            publishStatus: PublishStatus.Published);

        var commandResult = UpdateCategoryCommand.Create(
            categoryId: categoryBefore.Id,
            rowVersion: categoryBefore.RowVersion,
            publishStatus: "draft");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<CategoryErrors.PublishStatusTransitionNotAllowed>();
        error.CurrentStatus.Should().Be("published");
        error.AttemptedStatus.Should().Be("draft");

        var categoryAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryBefore.Id, ct));

        categoryAfter.PublishStatus.Should().Be(PublishStatus.Published);
        categoryAfter.RowVersion.Should().Be(categoryBefore.RowVersion);
    }
}
