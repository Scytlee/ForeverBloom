using ForeverBloom.Application.Categories.Commands.ArchiveCategory;
using ForeverBloom.Application.Categories.Commands.CreateCategory;
using ForeverBloom.Application.Categories.Commands.ReslugCategory;
using ForeverBloom.Application.Categories.Commands.UpdateCategory;
using ForeverBloom.Application.Products.Commands.ArchiveProduct;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Application.Products.Commands.ReslugProduct;
using ForeverBloom.Application.Products.Commands.UpdateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.Testing.Result;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ForeverBloom.Testing.Integration.Seeding;

public static class ApplicationFixtureSeeds
{
    public static async Task<Category> GivenCategoryAsync(
        this ApplicationTestFixture fixture,
        string? name = null,
        string? description = null,
        string? slug = null,
        string? imagePath = null,
        string? imageAltText = null,
        long? parentCategoryId = null,
        int displayOrder = 0,
        PublishStatus? publishStatus = null,
        string[]? slugHistory = null,
        bool isArchived = false,
        DateTimeOffset? creationTimestamp = null,
        DateTimeOffset? archivalTimestamp = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        // Validate that slug parameter is provided when slugHistory is used
        if (slugHistory is not null && slug is null)
        {
            throw new ArgumentException(
                $"{nameof(slug)} parameter is required when {nameof(slugHistory)} is provided.",
                nameof(slug));
        }

        var cancellationToken = TestContext.Current.CancellationToken;
        var seedToken = Guid.NewGuid().ToString("N")[..6];

        name ??= $"Category-{seedToken}";
        slug ??= $"cat-{seedToken}";

        string[] slugSequence = slugHistory is { Length: > 0 }
            ? [.. slugHistory, slug]
            : [slug];

        // Step 1: Create category
        if (creationTimestamp is not null)
        {
            fixture.TimeProvider.FreezeAt(creationTimestamp.Value);
        }

        var commandResult = CreateCategoryCommand.Create(
            name: name,
            slug: slugSequence[0],
            description: description,
            imagePath: imagePath,
            imageAltText: imageAltText,
            parentCategoryId: parentCategoryId,
            displayOrder: displayOrder);
        commandResult.Should().BeSuccess();

        var result = await fixture.SendAsync(commandResult.Value!, creationTimestamp, cancellationToken);
        result.Should().BeSuccess();

        var categoryId = result.Value!.CategoryId;

        // Fetch once to get initial RowVersion
        var category = await fixture.ExecuteDbContextAsync(
            (db, ct) => db.Set<Category>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == categoryId, ct),
            cancellationToken);

        var currentRowVersion = category.RowVersion;

        // Step 2: Update publish status if provided
        if (publishStatus is not null && publishStatus != PublishStatus.Draft)
        {
            var updateCommandResult = UpdateCategoryCommand.Create(
                categoryId: categoryId,
                rowVersion: currentRowVersion,
                publishStatus: Optional<string>.FromValue(publishStatus.Name));
            updateCommandResult.Should().BeSuccess();

            var updateResult = await fixture.SendAsync(updateCommandResult.Value!, creationTimestamp, cancellationToken);
            updateResult.Should().BeSuccess();

            currentRowVersion = updateResult.Value!.RowVersion;
        }

        // Step 3: Reslug through remaining sequence items (starting from index 1)
        for (var i = 1; i < slugSequence.Length; i++)
        {
            var reslugCommandResult = ReslugCategoryCommand.Create(
                categoryId: categoryId,
                rowVersion: currentRowVersion,
                newSlug: slugSequence[i]);
            reslugCommandResult.Should().BeSuccess();

            var reslugResult = await fixture.SendAsync(reslugCommandResult.Value!, creationTimestamp, cancellationToken);
            reslugResult.Should().BeSuccess();

            currentRowVersion = reslugResult.Value!.RowVersion;
        }

        if (creationTimestamp is not null)
        {
            fixture.TimeProvider.Unfreeze();
        }

        // Step 4: Archive if requested
        if (isArchived)
        {
            if (archivalTimestamp is not null)
            {
                fixture.TimeProvider.FreezeAt(archivalTimestamp.Value);
            }

            var archiveCommandResult = ArchiveCategoryCommand.Create(categoryId, currentRowVersion);
            archiveCommandResult.Should().BeSuccess();

            var archiveResult = await fixture.SendAsync(archiveCommandResult.Value!, archivalTimestamp, cancellationToken);
            archiveResult.Should().BeSuccess();

            if (archivalTimestamp is not null)
            {
                fixture.TimeProvider.Unfreeze();
            }
        }

        // Fetch final state once at the end
        await fixture.ExecuteDbContextAsync(
            (db, ct) => db.Entry(category).ReloadAsync(ct),
            cancellationToken);

        return category;
    }

    public static async Task<Category> ArchiveExistingCategoryAsync(
        this ApplicationTestFixture fixture,
        Category category,
        DateTimeOffset? archivalTimestamp = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(category);

        if (archivalTimestamp is not null)
        {
            fixture.TimeProvider.FreezeAt(archivalTimestamp.Value);
        }

        var cancellationToken = TestContext.Current.CancellationToken;
        var commandResult = ArchiveCategoryCommand.Create(category.Id, category.RowVersion);
        commandResult.Should().BeSuccess();

        var archiveResult = await fixture.SendAsync(commandResult.Value!, archivalTimestamp, cancellationToken);
        archiveResult.Should().BeSuccess();

        if (archivalTimestamp is not null)
        {
            fixture.TimeProvider.Unfreeze();
        }

        await fixture.ExecuteDbContextAsync(
            (db, ct) => db.Entry(category).ReloadAsync(ct),
            cancellationToken);

        return category;
    }

    public static async Task<Product> GivenProductAsync(
        this ApplicationTestFixture fixture,
        long categoryId,
        string? name = null,
        string? slug = null,
        string? seoTitle = null,
        string? fullDescription = null,
        string? metaDescription = null,
        decimal? price = null,
        bool isFeatured = false,
        ProductAvailabilityStatus? availabilityStatus = null,
        IReadOnlyCollection<CreateProductCommandImage>? images = null,
        PublishStatus? publishStatus = null,
        string[]? slugHistory = null,
        bool isArchived = false,
        DateTimeOffset? creationTimestamp = null,
        DateTimeOffset? archivalTimestamp = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        // Validate that slug parameter is provided when slugHistory is used
        if (slugHistory is not null && slug is null)
        {
            throw new ArgumentException(
                $"{nameof(slug)} parameter is required when {nameof(slugHistory)} is provided.",
                nameof(slug));
        }

        var cancellationToken = TestContext.Current.CancellationToken;
        var seedToken = Guid.NewGuid().ToString("N")[..6];

        name ??= $"Product-{seedToken}";
        slug ??= $"product-{seedToken}";

        string[] slugSequence = slugHistory is { Length: > 0 }
            ? [.. slugHistory, slug]
            : [slug];

        // Step 1: Create product
        if (creationTimestamp is not null)
        {
            fixture.TimeProvider.FreezeAt(creationTimestamp.Value);
        }

        var commandResult = CreateProductCommand.Create(
            name: name,
            slug: slugSequence[0],
            categoryId: categoryId,
            availabilityStatus: (availabilityStatus ?? ProductAvailabilityStatus.ComingSoon).Name,
            isFeatured: isFeatured,
            seoTitle: seoTitle,
            fullDescription: fullDescription,
            metaDescription: metaDescription,
            price: price,
            images: images?.ToArray());
        commandResult.Should().BeSuccess();

        var result = await fixture.SendAsync(commandResult.Value!, creationTimestamp, cancellationToken);
        result.Should().BeSuccess();

        var productId = result.Value!.Id;

        // Fetch once to get initial RowVersion
        var product = await fixture.ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(p => p.Id == productId, ct),
            cancellationToken);

        var currentRowVersion = product.RowVersion;

        // Step 2: Update publish status if provided
        if (publishStatus is not null && publishStatus != PublishStatus.Draft)
        {
            var updateCommandResult = UpdateProductCommand.Create(
                productId: productId,
                rowVersion: currentRowVersion,
                publishStatus: Optional<string>.FromValue(publishStatus.Name));
            updateCommandResult.Should().BeSuccess();

            var updateResult = await fixture.SendAsync(updateCommandResult.Value!, creationTimestamp, cancellationToken);
            updateResult.Should().BeSuccess();

            currentRowVersion = updateResult.Value!.RowVersion;
        }

        // Step 3: Reslug through remaining sequence items (starting from index 1)
        for (var i = 1; i < slugSequence.Length; i++)
        {
            var reslugCommandResult = ReslugProductCommand.Create(
                productId: productId,
                rowVersion: currentRowVersion,
                newSlug: slugSequence[i]);
            reslugCommandResult.Should().BeSuccess();

            var reslugResult = await fixture.SendAsync(reslugCommandResult.Value!, creationTimestamp, cancellationToken);
            reslugResult.Should().BeSuccess();

            currentRowVersion = reslugResult.Value!.RowVersion;
        }

        if (creationTimestamp is not null)
        {
            fixture.TimeProvider.Unfreeze();
        }

        // Step 4: Archive if requested
        if (isArchived)
        {
            if (archivalTimestamp is not null)
            {
                fixture.TimeProvider.FreezeAt(archivalTimestamp.Value);
            }

            var archiveCommandResult = ArchiveProductCommand.Create(productId, currentRowVersion);
            archiveCommandResult.Should().BeSuccess();

            var archiveResult = await fixture.SendAsync(archiveCommandResult.Value!, archivalTimestamp, cancellationToken);
            archiveResult.Should().BeSuccess();

            if (archivalTimestamp is not null)
            {
                fixture.TimeProvider.Unfreeze();
            }
        }

        // Fetch final state once at the end
        await fixture.ExecuteDbContextAsync(
            (db, ct) => db.Entry(product).ReloadAsync(ct),
            cancellationToken);

        return product;
    }
}
