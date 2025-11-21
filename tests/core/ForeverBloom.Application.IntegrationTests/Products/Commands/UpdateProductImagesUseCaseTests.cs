using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Application.Products.Commands.UpdateProductImages;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Products.Commands;

public sealed class UpdateProductImagesUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task UpdateProductImages_ShouldCreateNewImages_WhenCreateOperationsProvided()
    {
        var category = await Fixture.GivenCategoryAsync();
        var existingImages = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-existing.jpg", $"Existing {TestToken}", true, 1)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        var newImages = new[]
        {
            new UpdateProductImagesCommand.CreateImageOperation(
                Source: $"/images/{TestToken}-new1.jpg",
                AltText: $"New Image 1 {TestToken}",
                IsPrimary: false,
                DisplayOrder: 2),
            new UpdateProductImagesCommand.CreateImageOperation(
                Source: $"/images/{TestToken}-new2.jpg",
                AltText: $"New Image 2 {TestToken}",
                IsPrimary: false,
                DisplayOrder: 3)
        };

        var updateTimestamp = new DateTimeOffset(2025, 3, 15, 10, 30, 0, TimeSpan.Zero);

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            imagesToCreate: newImages);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, updateTimestamp, CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Images.Should().HaveCount(3);
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.Images.Should().HaveCount(3);
        productAfter.RowVersion.Should().Be(payload.RowVersion);
        productAfter.UpdatedAt.Should().Be(updateTimestamp);

        var newImage1 = productAfter.Images.Single(i => i.Image.Source.Value == newImages[0].Source);
        newImage1.Image.Should().Match(newImages[0].Source, newImages[0].AltText);
        newImage1.IsPrimary.Should().Be(newImages[0].IsPrimary);
        newImage1.DisplayOrder.Should().Be(newImages[0].DisplayOrder);

        var newImage2 = productAfter.Images.Single(i => i.Image.Source.Value == newImages[1].Source);
        newImage2.Image.Should().Match(newImages[1].Source, newImages[1].AltText);
        newImage2.IsPrimary.Should().Be(newImages[1].IsPrimary);
        newImage2.DisplayOrder.Should().Be(newImages[1].DisplayOrder);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldUpdateExistingImages_WhenUpdateOperationsProvided()
    {
        var category = await Fixture.GivenCategoryAsync();
        var existingImages = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1),
            new CreateProductCommandImage($"/images/{TestToken}-2.jpg", $"Image 2 {TestToken}", false, 2),
            new CreateProductCommandImage($"/images/{TestToken}-3.jpg", $"Image 3 {TestToken}", false, 3)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        var productWithImages = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        var productImages = productWithImages.Images.OrderBy(i => i.DisplayOrder).ToList();

        var updateOperations = new[]
        {
            new UpdateProductImagesCommand.UpdateImageOperation(
                Id: productImages[0].Id,
                AltText: $"Updated Alt Text {TestToken}",
                IsPrimary: false,
                DisplayOrder: 10),
            new UpdateProductImagesCommand.UpdateImageOperation(
                Id: productImages[1].Id,
                AltText: $"Updated Alt 2 {TestToken}",
                IsPrimary: true,
                DisplayOrder: 1)
        };

        var updateTimestamp = new DateTimeOffset(2025, 3, 15, 10, 30, 0, TimeSpan.Zero);

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            imagesToUpdate: updateOperations);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, updateTimestamp, CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Images.Should().HaveCount(3);
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.Images.Should().HaveCount(3);
        productAfter.RowVersion.Should().Be(payload.RowVersion);
        productAfter.UpdatedAt.Should().Be(updateTimestamp);

        var updatedImage1 = productAfter.Images.Single(i => i.Id == productImages[0].Id);
        updatedImage1.Image.Should().Match(productImages[0].Image.Source.Value, updateOperations[0].AltText.Value);
        updatedImage1.IsPrimary.Should().Be(updateOperations[0].IsPrimary.Value);
        updatedImage1.DisplayOrder.Should().Be(updateOperations[0].DisplayOrder.Value);

        var updatedImage2 = productAfter.Images.Single(i => i.Id == productImages[1].Id);
        updatedImage2.Image.Should().Match(productImages[1].Image.Source.Value, updateOperations[1].AltText.Value);
        updatedImage2.IsPrimary.Should().Be(updateOperations[1].IsPrimary.Value);
        updatedImage2.DisplayOrder.Should().Be(updateOperations[1].DisplayOrder.Value);

        var unchangedImage = productAfter.Images.Single(i => i.Id == productImages[2].Id);
        unchangedImage.Image.Should().Match(productImages[2].Image.Source.Value, productImages[2].Image.AltText);
        unchangedImage.IsPrimary.Should().Be(productImages[2].IsPrimary);
        unchangedImage.DisplayOrder.Should().Be(productImages[2].DisplayOrder);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldDeleteImages_WhenDeleteOperationsProvided()
    {
        var category = await Fixture.GivenCategoryAsync();
        var existingImages = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1),
            new CreateProductCommandImage($"/images/{TestToken}-2.jpg", $"Image 2 {TestToken}", false, 2),
            new CreateProductCommandImage($"/images/{TestToken}-3.jpg", $"Image 3 {TestToken}", false, 3)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        var productWithImages = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        var productImages = productWithImages.Images.OrderBy(i => i.DisplayOrder).ToList();

        var imagesToDelete = new[] { productImages[1].Id, productImages[2].Id };

        var updateTimestamp = new DateTimeOffset(2025, 3, 15, 10, 30, 0, TimeSpan.Zero);

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            imagesToDelete: imagesToDelete);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, updateTimestamp, CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Images.Should().ContainSingle();
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.Images.Should().ContainSingle();
        productAfter.RowVersion.Should().Be(payload.RowVersion);
        productAfter.UpdatedAt.Should().Be(updateTimestamp);

        var remainingImage = productAfter.Images.Single();
        remainingImage.Id.Should().Be(productImages[0].Id);
        remainingImage.Image.Should().Match(existingImages[0].Source, existingImages[0].AltText);
        remainingImage.IsPrimary.Should().Be(existingImages[0].IsPrimary);
        remainingImage.DisplayOrder.Should().Be(existingImages[0].DisplayOrder);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldPerformMixedOperations_WhenAllOperationTypesProvided()
    {
        var category = await Fixture.GivenCategoryAsync();
        var existingImages = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1),
            new CreateProductCommandImage($"/images/{TestToken}-2.jpg", $"Image 2 {TestToken}", false, 2),
            new CreateProductCommandImage($"/images/{TestToken}-3.jpg", $"Image 3 {TestToken}", false, 3)
        };

        // Product arranged with images 1, 2, 3
        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        var productWithImages = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        var productImages = productWithImages.Images.OrderBy(i => i.DisplayOrder).ToList();

        // Creating image 4
        var createOperations = new[]
        {
            new UpdateProductImagesCommand.CreateImageOperation(
                Source: $"/images/{TestToken}-new.jpg",
                AltText: $"New Image {TestToken}",
                IsPrimary: false,
                DisplayOrder: 4)
        };

        // Updating image 1
        var updateOperations = new[]
        {
            new UpdateProductImagesCommand.UpdateImageOperation(
                Id: productImages[0].Id,
                AltText: $"Updated Primary {TestToken}",
                IsPrimary: Optional<bool>.Unset,
                DisplayOrder: Optional<int>.Unset)
        };

        // Deleting image 3
        var deleteOperations = new[] { productImages[2].Id };

        var updateTimestamp = new DateTimeOffset(2025, 3, 15, 10, 30, 0, TimeSpan.Zero);

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            imagesToCreate: createOperations,
            imagesToUpdate: updateOperations,
            imagesToDelete: deleteOperations);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, updateTimestamp, CancellationToken);

        // Final result: images 1, 2 updated, 4
        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Images.Should().HaveCount(3);
        payload.RowVersion.Should().BeGreaterThan(product.RowVersion);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.Images.Should().HaveCount(3);
        productAfter.RowVersion.Should().Be(payload.RowVersion);
        productAfter.UpdatedAt.Should().Be(updateTimestamp);

        var updatedImage = productAfter.Images.Single(i => i.Id == productImages[0].Id);
        updatedImage.Image.AltText.Should().Be(updateOperations[0].AltText.Value);
        updatedImage.IsPrimary.Should().Be(existingImages[0].IsPrimary);
        updatedImage.DisplayOrder.Should().Be(existingImages[0].DisplayOrder);

        var unchangedImage = productAfter.Images.Single(i => i.Id == productImages[1].Id);
        unchangedImage.Image.Should().Match(existingImages[1].Source, existingImages[1].AltText);
        unchangedImage.IsPrimary.Should().Be(existingImages[1].IsPrimary);
        unchangedImage.DisplayOrder.Should().Be(existingImages[1].DisplayOrder);

        var newImage = productAfter.Images.Single(i => i.Image.Source.Value == createOperations[0].Source);
        newImage.Image.Should().Match(createOperations[0].Source, createOperations[0].AltText);
        newImage.IsPrimary.Should().Be(createOperations[0].IsPrimary);
        newImage.DisplayOrder.Should().Be(createOperations[0].DisplayOrder);

        productAfter.Images.Should().NotContain(i => i.Id == productImages[2].Id);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldClearAltText_WhenNullProvided()
    {
        var category = await Fixture.GivenCategoryAsync();
        var existingImages = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        var productWithImages = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        var productImages = productWithImages.Images.OrderBy(i => i.DisplayOrder).ToList();

        var updateOperations = new[]
        {
            new UpdateProductImagesCommand.UpdateImageOperation(
                Id: productImages[0].Id,
                AltText: (string?)null,
                IsPrimary: Optional<bool>.Unset,
                DisplayOrder: Optional<int>.Unset)
        };

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            imagesToUpdate: updateOperations);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var payload = result.Value!;
        payload.Images.Should().HaveCount(1);

        var clearedImage = payload.Images.Single(i => i.Id == productImages[0].Id);
        clearedImage.AltText.Should().BeNull();

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        var clearedImageAfter = productAfter.Images.Single(i => i.Id == productImages[0].Id);
        clearedImageAfter.Image.AltText.Should().BeNull();
        clearedImageAfter.IsPrimary.Should().Be(existingImages[0].IsPrimary);
        clearedImageAfter.DisplayOrder.Should().Be(existingImages[0].DisplayOrder);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldFail_WhenProductDoesNotExist()
    {
        const long nonExistentProductId = 999_999L;

        var commandResult = UpdateProductImagesCommand.Create(
            productId: nonExistentProductId,
            rowVersion: 1);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.NotFoundById>();
        error.Id.Should().Be(nonExistentProductId);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldFail_WhenRowVersionMismatch()
    {
        var category = await Fixture.GivenCategoryAsync();
        var existingImages = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        var staleRowVersion = product.RowVersion + 1;

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: staleRowVersion,
            imagesToDelete: [1]);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.RowVersion.Should().Be(product.RowVersion);
        productAfter.Images.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldFail_WhenImageNotFound()
    {
        var category = await Fixture.GivenCategoryAsync();
        var existingImages = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        const long nonExistentImageId = 999_999L;

        var updateOperations = new[]
        {
            new UpdateProductImagesCommand.UpdateImageOperation(
                Id: nonExistentImageId,
                AltText: $"New Alt {TestToken}",
                IsPrimary: Optional<bool>.Unset,
                DisplayOrder: Optional<int>.Unset)
        };

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            imagesToUpdate: updateOperations);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.ImageNotFound>();
        error.Id.Should().Be(nonExistentImageId);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.RowVersion.Should().Be(product.RowVersion);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldFail_WhenNoPrimaryImageRemains()
    {
        var category = await Fixture.GivenCategoryAsync();
        var existingImages = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1),
            new CreateProductCommandImage($"/images/{TestToken}-2.jpg", $"Image 2 {TestToken}", false, 2)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        var productWithImages = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        var productImages = productWithImages.Images.OrderBy(i => i.DisplayOrder).ToList();

        var updateOperations = new[]
        {
            new UpdateProductImagesCommand.UpdateImageOperation(
                Id: productImages[0].Id,
                AltText: Optional<string?>.Unset,
                IsPrimary: false,
                DisplayOrder: Optional<int>.Unset)
        };

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            imagesToUpdate: updateOperations);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ProductErrors.NoPrimaryImage>();

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.RowVersion.Should().Be(product.RowVersion);
        productAfter.Images.Single(i => i.Id == productImages[0].Id).IsPrimary.Should().Be(existingImages[0].IsPrimary);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldFail_WhenMultiplePrimaryImagesResult()
    {
        var category = await Fixture.GivenCategoryAsync();
        var existingImages = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1),
            new CreateProductCommandImage($"/images/{TestToken}-2.jpg", $"Image 2 {TestToken}", false, 2),
            new CreateProductCommandImage($"/images/{TestToken}-3.jpg", $"Image 3 {TestToken}", false, 3)
        };

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        var productWithImages = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        var productImages = productWithImages.Images.OrderBy(i => i.DisplayOrder).ToList();

        var updateOperations = new[]
        {
            new UpdateProductImagesCommand.UpdateImageOperation(
                Id: productImages[1].Id,
                AltText: Optional<string?>.Unset,
                IsPrimary: true,
                DisplayOrder: Optional<int>.Unset),
            new UpdateProductImagesCommand.UpdateImageOperation(
                Id: productImages[2].Id,
                AltText: Optional<string?>.Unset,
                IsPrimary: true,
                DisplayOrder: Optional<int>.Unset)
        };

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            imagesToUpdate: updateOperations);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.MultiplePrimaryImages>();
        error.Indices.Should().BeEquivalentTo([0, 1, 2]);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.RowVersion.Should().Be(product.RowVersion);
        productAfter.Images.Count(i => i.IsPrimary).Should().Be(1);
    }

    [Fact]
    public async Task UpdateProductImages_ShouldFail_WhenTooManyImages()
    {
        var category = await Fixture.GivenCategoryAsync();

        // Create product with 19 images (just under the limit)
        var existingImages = Enumerable.Range(1, 19)
            .Select(i => new CreateProductCommandImage(
                $"/images/{TestToken}-{i}.jpg",
                $"Image {i} {TestToken}",
                i == 1,
                i))
            .ToArray();

        var product = await Fixture.GivenProductAsync(
            categoryId: category.Id,
            name: $"Product-{TestToken}",
            slug: $"product-{TestToken}",
            images: existingImages);

        // Try to create 2 more images (would result in 21 total, exceeding limit of 20)
        var newImages = new[]
        {
            new UpdateProductImagesCommand.CreateImageOperation(
                Source: $"/images/{TestToken}-20.jpg",
                AltText: $"Image 20 {TestToken}",
                IsPrimary: false,
                DisplayOrder: 20),
            new UpdateProductImagesCommand.CreateImageOperation(
                Source: $"/images/{TestToken}-21.jpg",
                AltText: $"Image 21 {TestToken}",
                IsPrimary: false,
                DisplayOrder: 21)
        };

        var commandResult = UpdateProductImagesCommand.Create(
            productId: product.Id,
            rowVersion: product.RowVersion,
            imagesToCreate: newImages);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.TooManyImages>();
        error.Count.Should().Be(existingImages.Length + newImages.Length);

        var productAfter = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == product.Id, ct));

        productAfter.RowVersion.Should().Be(product.RowVersion);
        productAfter.Images.Should().HaveCount(existingImages.Length);
    }
}
