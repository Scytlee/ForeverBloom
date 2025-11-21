using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.SlugRegistry;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Seeding;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.IntegrationTests.Products.Commands;

public sealed class CreateProductUseCaseTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task CreateProduct_ShouldPersistProductAndSlug_WhenOnlyRequiredFieldsProvided()
    {
        var category = await Fixture.GivenCategoryAsync();
        var name = $"Bouquet-{TestToken}";
        var slug = $"bouquet-{TestToken}";

        var commandResult = CreateProductCommand.Create(
            name: name,
            slug: slug,
            categoryId: category.Id);
        commandResult.Should().BeSuccess();

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var productId = result.Value!.Id;
        productId.Should().BeGreaterThan(0);

        var product = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == productId, ct));

        product.Id.Should().Be(productId);
        product.Name.Should().HaveValue(name);
        product.CurrentSlug.Should().HaveValue(slug);
        product.CategoryId.Should().Be(category.Id);
        product.SeoTitle.Should().BeNull();
        product.FullDescription.Should().BeNull();
        product.MetaDescription.Should().BeNull();
        product.Price.Should().BeNull();
        product.IsFeatured.Should().BeFalse();
        product.PublishStatus.Should().Be(PublishStatus.Draft);
        product.Availability.Should().Be(ProductAvailabilityStatus.ComingSoon);
        product.Images.Should().BeEmpty();
        product.CreatedAt.Should().Be(actionTimestamp);
        product.UpdatedAt.Should().Be(actionTimestamp);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Product && r.EntityId == productId)
                .ToListAsync(ct));

        slugRegistrations.Should().ContainSingle();
        var slugRegistration = slugRegistrations.Single();
        slugRegistration.Slug.Should().HaveValue(slug);
        slugRegistration.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_ShouldPersistAllOptionalFields_WhenAllFieldsProvided()
    {
        var category = await Fixture.GivenCategoryAsync();

        var name = $"Deluxe-{TestToken}";
        var slug = $"deluxe-{TestToken}";
        var seoTitle = $"Artisan {TestToken}";
        var fullDescription = $"<p>Hand-tied bouquet {TestToken}</p>";
        var metaDescription = $"Meta description {TestToken}";
        const decimal price = 149.99m;
        var images = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-primary.jpg", $"Primary {TestToken}", true, 1),
            new CreateProductCommandImage($"/images/{TestToken}-secondary.jpg", $"Secondary {TestToken}", false, 2)
        };

        var commandResult = CreateProductCommand.Create(
            name: name,
            slug: slug,
            categoryId: category.Id,
            isFeatured: true,
            availabilityStatus: "available",
            seoTitle: seoTitle,
            fullDescription: fullDescription,
            metaDescription: metaDescription,
            price: price,
            images: images);
        commandResult.Should().BeSuccess();

        var actionTimestamp = Fixture.TimeProvider.Freeze();
        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var productId = result.Value!.Id;
        productId.Should().BeGreaterThan(0);

        var product = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == productId, ct));

        product.Id.Should().Be(productId);
        product.Name.Should().HaveValue(name);
        product.CurrentSlug.Should().HaveValue(slug);
        product.CategoryId.Should().Be(category.Id);
        product.SeoTitle.Should().HaveValue(seoTitle);
        product.FullDescription.Should().HaveValue(fullDescription);
        product.MetaDescription.Should().HaveValue(metaDescription);
        product.Price.Should().HaveValue(price);
        product.IsFeatured.Should().BeTrue();
        product.PublishStatus.Should().Be(PublishStatus.Draft);
        product.Availability.Should().Be(ProductAvailabilityStatus.Available);
        product.CreatedAt.Should().Be(actionTimestamp);
        product.UpdatedAt.Should().Be(actionTimestamp);

        product.Images.Should().HaveCount(2);
        var primaryImage = product.Images.Single(i => i.IsPrimary);
        primaryImage.DisplayOrder.Should().Be(images[0].DisplayOrder);
        primaryImage.Image.Should().Match(images[0].Source, images[0].AltText);

        var secondaryImage = product.Images.Single(i => !i.IsPrimary);
        secondaryImage.DisplayOrder.Should().Be(images[1].DisplayOrder);
        secondaryImage.Image.Should().Match(images[1].Source, images[1].AltText);

        var slugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Product && r.EntityId == productId)
                .ToListAsync(ct));

        slugRegistrations.Should().ContainSingle();
        slugRegistrations.Single().Slug.Should().HaveValue(slug);
        slugRegistrations.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_ShouldCreateProductWithSingleImage_WhenOneImageProvided()
    {
        var category = await Fixture.GivenCategoryAsync();

        var images = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-single.jpg", $"Single {TestToken}", true, 1)
        };

        var commandResult = CreateProductCommand.Create(
            name: $"Single-{TestToken}",
            slug: $"single-{TestToken}",
            categoryId: category.Id,
            images: images);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeSuccess();
        var productId = result.Value!.Id;

        var product = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>()
                .AsNoTracking()
                .Include(p => p.Images)
                .SingleAsync(p => p.Id == productId, ct));

        product.Images.Should().ContainSingle();
        var image = product.Images.Single();
        image.IsPrimary.Should().BeTrue();
        image.DisplayOrder.Should().Be(images[0].DisplayOrder);
        image.Image.Should().Match(images[0].Source, images[0].AltText);
    }

    [Fact]
    public async Task CreateProduct_ShouldFail_WhenSlugAlreadyUsedByAnotherProduct()
    {
        var category = await Fixture.GivenCategoryAsync();
        var duplicateSlug = $"shared-product-{TestToken}";

        await Fixture.GivenProductAsync(
            categoryId: category.Id,
            slug: duplicateSlug);

        var commandResult = CreateProductCommand.Create(
            name: $"Another-{TestToken}",
            slug: duplicateSlug,
            categoryId: category.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.SlugNotAvailable>();
        error.Slug.Should().Be(duplicateSlug);

        var productCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>().CountAsync(ct));
        productCount.Should().Be(1);

        var productSlugRegistrations = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .AsNoTracking()
                .Where(r => r.EntityType == EntityType.Product)
                .ToListAsync(ct));

        productSlugRegistrations.Should().HaveCount(1);
        productSlugRegistrations.Single().Slug.Should().HaveValue(duplicateSlug);
    }

    [Fact]
    public async Task CreateProduct_ShouldFail_WhenSlugAlreadyUsedByCategory()
    {
        var sharedSlug = $"shared-{TestToken}";

        await Fixture.GivenCategoryAsync(
            name: $"Category-{TestToken}",
            slug: sharedSlug);

        var categoryForProduct = await Fixture.GivenCategoryAsync();

        var commandResult = CreateProductCommand.Create(
            name: $"Product-{TestToken}",
            slug: sharedSlug,
            categoryId: categoryForProduct.Id);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.SlugNotAvailable>();
        error.Slug.Should().Be(sharedSlug);

        var productCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>().CountAsync(ct));
        productCount.Should().Be(0);

        var slugRegistrationCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .CountAsync(r => r.Slug == sharedSlug, cancellationToken: ct));
        slugRegistrationCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateProduct_ShouldFail_WhenCategoryDoesNotExist()
    {
        const long missingCategoryId = 9_999_999;

        var commandResult = CreateProductCommand.Create(
            name: $"Missing-{TestToken}",
            slug: $"missing-{TestToken}",
            categoryId: missingCategoryId);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.CategoryNotFound>();
        error.CategoryId.Should().Be(missingCategoryId);

        var productCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>().CountAsync(ct));
        productCount.Should().Be(0);

        var productSlugCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .CountAsync(r => r.EntityType == EntityType.Product, ct));
        productSlugCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateProduct_ShouldFail_WhenMultiplePrimaryImagesProvided()
    {
        var category = await Fixture.GivenCategoryAsync();

        var images = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", true, 1),
            new CreateProductCommandImage($"/images/{TestToken}-2.jpg", $"Image 2 {TestToken}", true, 2)
        };

        var commandResult = CreateProductCommand.Create(
            name: $"Multi-{TestToken}",
            slug: $"multi-{TestToken}",
            categoryId: category.Id,
            images: images);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.MultiplePrimaryImages>();
        error.Indices.Should().BeEquivalentTo([0, 1]);

        var productCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>().CountAsync(ct));
        productCount.Should().Be(0);

        var productSlugRegistrationCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .CountAsync(r => r.EntityType == EntityType.Product, ct));
        productSlugRegistrationCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateProduct_ShouldFail_WhenImagesProvidedButNoPrimary()
    {
        var category = await Fixture.GivenCategoryAsync();

        var images = new[]
        {
            new CreateProductCommandImage($"/images/{TestToken}-1.jpg", $"Image 1 {TestToken}", false, 1),
            new CreateProductCommandImage($"/images/{TestToken}-2.jpg", $"Image 2 {TestToken}", false, 2)
        };

        var commandResult = CreateProductCommand.Create(
            name: $"NoPrimary-{TestToken}",
            slug: $"no-primary-{TestToken}",
            categoryId: category.Id,
            images: images);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        result.Should().HaveError<ProductErrors.NoPrimaryImage>();

        var productCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>().CountAsync(ct));
        productCount.Should().Be(0);

        var productSlugRegistrationCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .CountAsync(r => r.EntityType == EntityType.Product, ct));
        productSlugRegistrationCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateProduct_ShouldFail_WhenTooManyImagesProvided()
    {
        var category = await Fixture.GivenCategoryAsync();

        // Create 21 images (exceeds max of 20)
        var images = Enumerable.Range(1, 21)
            .Select(i => new CreateProductCommandImage(
                $"/images/{TestToken}-{i}.jpg",
                $"Image {i} {TestToken}",
                i == 1, // First image is primary
                i))
            .ToArray();

        var commandResult = CreateProductCommand.Create(
            name: $"TooMany-{TestToken}",
            slug: $"too-many-{TestToken}",
            categoryId: category.Id,
            images: images);
        commandResult.Should().BeSuccess();

        var result = await SendAsync(commandResult.Value!, cancellationToken: CancellationToken);

        result.Should().BeFailure();
        var error = result.Should().HaveError<ProductErrors.TooManyImages>();
        error.Count.Should().Be(21);

        var productCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<Product>().CountAsync(ct));
        productCount.Should().Be(0);

        var productSlugRegistrationCount = await ExecuteDbContextAsync(
            (db, ct) => db.Set<SlugRegistration>()
                .CountAsync(r => r.EntityType == EntityType.Product, ct));
        productSlugRegistrationCount.Should().Be(0);
    }
}
