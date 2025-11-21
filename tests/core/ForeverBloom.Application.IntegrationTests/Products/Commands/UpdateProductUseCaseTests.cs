using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Products.Commands.UpdateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Products.Commands;

public sealed class UpdateProductUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task UpdateProduct_ShouldUpdateAllMutableFields_WhenValidCommand()
    {
        var originalCategory = await Fixture.GivenCategoryAsync();
        var targetCategory = await Fixture.GivenCategoryAsync();

        var productBefore = await Fixture.GivenProductAsync(
            categoryId: originalCategory.Id,
            name: $"Original-{TestToken}",
            slug: $"original-{TestToken}",
            price: 49.99m,
            isFeatured: false,
            availabilityStatus: ProductAvailabilityStatus.ComingSoon);

        var updatedName = $"Updated-{TestToken}";
        var seoTitle = $"Updated Seo {TestToken}";
        var fullDescription = $"<p>Updated description {TestToken}</p>";
        var metaDescription = $"Updated meta {TestToken}";
        const decimal updatedPrice = 199.95m;

        var updateTimestamp = new DateTimeOffset(2025, 3, 15, 10, 30, 0, TimeSpan.Zero);

        var commandResult = UpdateProductCommand.Create(
            productId: productBefore.Id,
            rowVersion: productBefore.RowVersion,
            name: updatedName,
            seoTitle: seoTitle,
            fullDescription: fullDescription,
            metaDescription: metaDescription,
            categoryId: targetCategory.Id,
            price: updatedPrice,
            isFeatured: true,
            availability: "available",
            publishStatus: "published");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, updateTimestamp, CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Name.Should().Be(updatedName);
        payload.SeoTitle.Should().Be(seoTitle);
        payload.FullDescription.Should().Be(fullDescription);
        payload.MetaDescription.Should().Be(metaDescription);
        payload.CategoryId.Should().Be(targetCategory.Id);
        payload.Price.Should().Be(updatedPrice);
        payload.IsFeatured.Should().BeTrue();
        payload.Availability.Should().Be(ProductAvailabilityStatus.Available);
        payload.PublishStatus.Should().Be(PublishStatus.Published);
        payload.RowVersion.Should().BeGreaterThan(productBefore.RowVersion);
        payload.UpdatedAt.Should().Be(updateTimestamp);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.Name.Should().HaveValue(updatedName);
        productAfter.SeoTitle.Should().HaveValue(seoTitle);
        productAfter.FullDescription.Should().HaveValue(fullDescription);
        productAfter.MetaDescription.Should().HaveValue(metaDescription);
        productAfter.CategoryId.Should().Be(targetCategory.Id);
        productAfter.Price.Should().HaveValue(updatedPrice);
        productAfter.IsFeatured.Should().BeTrue();
        productAfter.Availability.Should().Be(ProductAvailabilityStatus.Available);
        productAfter.PublishStatus.Should().Be(PublishStatus.Published);
        productAfter.RowVersion.Should().Be(payload.RowVersion);
        productAfter.UpdatedAt.Should().Be(payload.UpdatedAt);
    }

    [Fact]
    public async Task UpdateProduct_ShouldClearNullableFields_WhenNullProvided()
    {
        var category = await Fixture.GivenCategoryAsync();
        const decimal initialPrice = 99.99m;

        var productBefore = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Clearable-{TestToken}",
            slug: $"clearable-{TestToken}",
            seoTitle: $"Seo {TestToken}",
            fullDescription: $"<p>Full {TestToken}</p>",
            metaDescription: $"Meta {TestToken}",
            price: initialPrice,
            isFeatured: true,
            availabilityStatus: ProductAvailabilityStatus.Available);

        var commandResult = UpdateProductCommand.Create(
            productId: productBefore.Id,
            rowVersion: productBefore.RowVersion,
            seoTitle: (string?)null,
            fullDescription: (string?)null,
            metaDescription: (string?)null,
            price: (decimal?)null,
            publishStatus: "hidden");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.SeoTitle.Should().BeNull();
        payload.FullDescription.Should().BeNull();
        payload.MetaDescription.Should().BeNull();
        payload.Price.Should().BeNull();
        payload.PublishStatus.Should().Be(PublishStatus.Hidden);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.SeoTitle.Should().BeNull();
        productAfter.FullDescription.Should().BeNull();
        productAfter.MetaDescription.Should().BeNull();
        productAfter.Price.Should().BeNull();
        productAfter.PublishStatus.Should().Be(PublishStatus.Hidden);
    }

    [Fact]
    public async Task UpdateProduct_ShouldUpdateOnlySomeFields_WhenPartialUpdateProvided()
    {
        var category = await Fixture.GivenCategoryAsync();

        var productBefore = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Partial-{TestToken}",
            slug: $"partial-{TestToken}",
            seoTitle: $"Original Seo {TestToken}",
            fullDescription: $"<p>Original description {TestToken}</p>",
            price: 75.00m,
            isFeatured: false,
            availabilityStatus: ProductAvailabilityStatus.ComingSoon);

        var newName = $"Updated-{TestToken}";
        const decimal newPrice = 125.50m;

        var commandResult = UpdateProductCommand.Create(
            productId: productBefore.Id,
            rowVersion: productBefore.RowVersion,
            name: newName,
            price: newPrice);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Name.Should().Be(newName);
        payload.Price.Should().Be(newPrice);
        payload.SeoTitle.Should().Be(productBefore.SeoTitle!.Value);
        payload.FullDescription.Should().Be(productBefore.FullDescription!.Value);
        payload.CategoryId.Should().Be(productBefore.CategoryId);
        payload.IsFeatured.Should().Be(productBefore.IsFeatured);
        payload.Availability.Should().Be(productBefore.Availability);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.Name.Should().HaveValue(newName);
        productAfter.Price.Should().HaveValue(newPrice);
        productAfter.SeoTitle.Should().HaveValue(productBefore.SeoTitle.Value);
        productAfter.FullDescription.Should().HaveValue(productBefore.FullDescription!.Value);
        productAfter.CategoryId.Should().Be(productBefore.CategoryId);
        productAfter.IsFeatured.Should().Be(productBefore.IsFeatured);
        productAfter.Availability.Should().Be(productBefore.Availability);
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturnSuccessWithoutPersisting_WhenNoFieldsChange()
    {
        var category = await Fixture.GivenCategoryAsync();

        var productBefore = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            price: 49.99m,
            isFeatured: true,
            availabilityStatus: ProductAvailabilityStatus.Available);

        var commandResult = UpdateProductCommand.Create(
            productId: productBefore.Id,
            rowVersion: productBefore.RowVersion);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.RowVersion.Should().Be(productBefore.RowVersion);
        payload.UpdatedAt.Should().Be(productBefore.UpdatedAt);
        payload.Name.Should().Be(productBefore.Name.Value);
        payload.Price.Should().Be(productBefore.Price?.Value);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.RowVersion.Should().Be(productBefore.RowVersion);
        productAfter.UpdatedAt.Should().Be(productBefore.UpdatedAt);
    }

    [Fact]
    public async Task UpdateProduct_ShouldFail_WhenProductDoesNotExist()
    {
        const long nonExistentProductId = 999_999_999;

        var commandResult = UpdateProductCommand.Create(
            productId: nonExistentProductId,
            rowVersion: 1,
            name: $"DoesNotMatter-{TestToken}");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundById>();
        error.Id.Should().Be(nonExistentProductId);
    }

    [Fact]
    public async Task UpdateProduct_ShouldFail_WhenRowVersionMismatch()
    {
        var category = await Fixture.GivenCategoryAsync();

        var productBefore = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Stale-{TestToken}",
            slug: $"stale-{TestToken}");

        var staleRowVersion = productBefore.RowVersion + 1;

        var commandResult = UpdateProductCommand.Create(
            productId: productBefore.Id,
            rowVersion: staleRowVersion,
            name: $"StaleUpdate-{TestToken}");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.RowVersion.Should().Be(productBefore.RowVersion);
        productAfter.Name.Should().HaveValue(productBefore.Name.Value);
    }

    [Fact]
    public async Task UpdateProduct_ShouldFail_WhenCategoryDoesNotExist()
    {
        var category = await Fixture.GivenCategoryAsync();

        var productBefore = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"MissingCat-{TestToken}",
            slug: $"missing-cat-{TestToken}");

        const long missingCategoryId = 999_999_999;

        var commandResult = UpdateProductCommand.Create(
            productId: productBefore.Id,
            rowVersion: productBefore.RowVersion,
            categoryId: missingCategoryId);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.CategoryNotFound>();
        error.CategoryId.Should().Be(missingCategoryId);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.CategoryId.Should().Be(productBefore.CategoryId);
        productAfter.RowVersion.Should().Be(productBefore.RowVersion);
    }

    [Fact]
    public async Task UpdateProduct_ShouldFail_WhenPublishStatusTransitionNotAllowed()
    {
        var category = await Fixture.GivenCategoryAsync();

        var productBefore = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Published-{TestToken}",
            slug: $"published-{TestToken}",
            publishStatus: PublishStatus.Published);

        var commandResult = UpdateProductCommand.Create(
            productId: productBefore.Id,
            rowVersion: productBefore.RowVersion,
            publishStatus: "draft");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.PublishStatusTransitionNotAllowed>();
        error.CurrentStatus.Should().Be("published");
        error.AttemptedStatus.Should().Be("draft");

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.PublishStatus.Should().Be(PublishStatus.Published);
        productAfter.RowVersion.Should().Be(productBefore.RowVersion);
    }

    [Fact]
    public async Task UpdateProduct_ShouldAllowValidPublishStatusTransition()
    {
        var category = await Fixture.GivenCategoryAsync();

        var productBefore = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Draft-{TestToken}",
            slug: $"draft-{TestToken}",
            publishStatus: PublishStatus.Draft);

        var commandResult = UpdateProductCommand.Create(
            productId: productBefore.Id,
            rowVersion: productBefore.RowVersion,
            publishStatus: "published");
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.PublishStatus.Should().Be(PublishStatus.Published);
        payload.RowVersion.Should().BeGreaterThan(productBefore.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productBefore.Id, ct));

        productAfter.PublishStatus.Should().Be(PublishStatus.Published);
        productAfter.RowVersion.Should().Be(payload.RowVersion);
    }
}
