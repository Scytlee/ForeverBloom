using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class ProductTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldSucceed_WithMinimalRequiredFields()
    {
        var name = ProductNameFactory.Create();
        var slug = SlugFactory.Create();
        const long categoryId = 1;

        var result = Product.Create(
            name,
            seoTitle: null,
            fullDescription: null,
            metaDescription: null,
            slug,
            categoryId,
            price: null,
            isFeatured: false,
            availabilityStatus: ProductAvailabilityStatus.ComingSoon,
            TestTimestamp);

        result.Should().BeSuccess();
        var product = result.Value!;
        product.Name.Should().Be(name);
        product.CurrentSlug.Should().Be(slug);
        product.CategoryId.Should().Be(categoryId);
        product.SeoTitle.Should().BeNull();
        product.FullDescription.Should().BeNull();
        product.MetaDescription.Should().BeNull();
        product.Price.Should().BeNull();
        product.IsFeatured.Should().BeFalse();
        product.PublishStatus.Should().Be(PublishStatus.Draft);
        product.Availability.Should().Be(ProductAvailabilityStatus.ComingSoon);
        product.Images.Should().BeEmpty();
        product.IsDeleted.Should().BeFalse();
        product.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSucceed_WithAllFieldsPopulated()
    {
        var name = ProductNameFactory.Create();
        var seoTitle = SeoTitleFactory.Create();
        var fullDescription = HtmlFragmentFactory.Create();
        var metaDescription = MetaDescriptionFactory.Create();
        var slug = SlugFactory.Create();
        const long categoryId = 5;
        var price = MoneyFactory.Create();
        var images = new[] { ProductImageFactory.Create(isPrimary: true) };

        var result = Product.Create(
            name,
            seoTitle,
            fullDescription,
            metaDescription,
            slug,
            categoryId,
            price,
            isFeatured: true,
            availabilityStatus: ProductAvailabilityStatus.Available,
            TestTimestamp,
            images);

        result.Should().BeSuccess();
        var product = result.Value!;
        product.Name.Should().Be(name);
        product.SeoTitle.Should().Be(seoTitle);
        product.FullDescription.Should().Be(fullDescription);
        product.MetaDescription.Should().Be(metaDescription);
        product.CurrentSlug.Should().Be(slug);
        product.CategoryId.Should().Be(categoryId);
        product.Price.Should().Be(price);
        product.IsFeatured.Should().BeTrue();
        product.PublishStatus.Should().Be(PublishStatus.Draft); // Always Draft on creation
        product.Availability.Should().Be(ProductAvailabilityStatus.Available);
        product.Images.Should().HaveCount(1);
        product.Images.First().Should().Be(images.First());
    }

    [Fact]
    public void Create_ShouldSucceed_WithEmptyImagesCollection()
    {
        var name = ProductNameFactory.Create();
        var slug = SlugFactory.Create();

        var result = Product.Create(
            name,
            seoTitle: null,
            fullDescription: null,
            metaDescription: null,
            slug,
            categoryId: 1,
            price: null,
            isFeatured: false,
            availabilityStatus: ProductAvailabilityStatus.ComingSoon,
            TestTimestamp,
            images: []);

        result.Should().BeSuccess();
        result.Value!.Images.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldSucceed_WithValidImagesCollection()
    {
        var name = ProductNameFactory.Create();
        var slug = SlugFactory.Create();
        var images = new[]
        {
            ProductImageFactory.Create("/images/img1.jpg", "Image 1", isPrimary: true, displayOrder: 0),
            ProductImageFactory.Create("/images/img2.jpg", "Image 2", isPrimary: false, displayOrder: 1)
        };

        var result = Product.Create(
            name,
            seoTitle: null,
            fullDescription: null,
            metaDescription: null,
            slug,
            categoryId: 1,
            price: null,
            isFeatured: false,
            availabilityStatus: ProductAvailabilityStatus.ComingSoon,
            TestTimestamp,
            images);

        result.Should().BeSuccess();
        var product = result.Value!;
        product.Images.Should().HaveCount(2);
        product.Images.Count(img => img.IsPrimary).Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_ShouldFail_ForInvalidCategoryId(long categoryId)
    {
        var name = ProductNameFactory.Create();
        var slug = SlugFactory.Create();

        var result = Product.Create(
            name,
            seoTitle: null,
            fullDescription: null,
            metaDescription: null,
            slug,
            categoryId,
            price: null,
            isFeatured: false,
            availabilityStatus: ProductAvailabilityStatus.ComingSoon,
            TestTimestamp);

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<ProductErrors.CategoryIdInvalid>();
        error.AttemptedId.Should().Be(categoryId);
    }

    [Fact]
    public void Create_ShouldFail_WhenNoPrimaryImage()
    {
        var name = ProductNameFactory.Create();
        var slug = SlugFactory.Create();
        var images = new[]
        {
            ProductImageFactory.Create("/images/test.jpg", "Test", isPrimary: false, displayOrder: 0)
        };

        var result = Product.Create(
            name,
            seoTitle: null,
            fullDescription: null,
            metaDescription: null,
            slug,
            categoryId: 1,
            price: null,
            isFeatured: false,
            availabilityStatus: ProductAvailabilityStatus.ComingSoon,
            TestTimestamp,
            images);

        result.Should().BeFailure();
        result.Should().HaveSingleError<ProductErrors.NoPrimaryImage>();
    }

    [Fact]
    public void Create_ShouldFail_WhenMultiplePrimaryImages()
    {
        var name = ProductNameFactory.Create();
        var slug = SlugFactory.Create();
        var images = new[]
        {
            ProductImageFactory.Create("/images/img1.jpg", "Image 1", isPrimary: true, displayOrder: 0),
            ProductImageFactory.Create("/images/img2.jpg", "Image 2", isPrimary: true, displayOrder: 1)
        };

        var result = Product.Create(
            name,
            seoTitle: null,
            fullDescription: null,
            metaDescription: null,
            slug,
            categoryId: 1,
            price: null,
            isFeatured: false,
            availabilityStatus: ProductAvailabilityStatus.ComingSoon,
            TestTimestamp,
            images);

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<ProductErrors.MultiplePrimaryImages>();
        error.PrimaryIndices.Should().BeEquivalentTo([0, 1]);
    }

    [Fact]
    public void Create_ShouldFail_WhenTooManyImages()
    {
        var name = ProductNameFactory.Create();
        var slug = SlugFactory.Create();
        var images = Enumerable.Range(1, 21)
            .Select(i => ProductImageFactory.Create(
                $"/images/img{i}.jpg",
                $"Image {i}",
                isPrimary: i == 1,
                displayOrder: i))
            .ToArray();

        var result = Product.Create(
            name,
            seoTitle: null,
            fullDescription: null,
            metaDescription: null,
            slug,
            categoryId: 1,
            price: null,
            isFeatured: false,
            availabilityStatus: ProductAvailabilityStatus.ComingSoon,
            TestTimestamp,
            images);

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<ProductErrors.TooManyImages>();
        error.AttemptedCount.Should().Be(21);
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingName()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var newName = ProductNameFactory.Create("Updated product");

        var result = product.Update(
            name: newName,
            seoTitle: default,
            fullDescription: default,
            metaDescription: default,
            categoryId: default,
            price: default,
            isFeatured: default,
            availability: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        product.Name.Should().Be(newName);
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingSeoTitle()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var newSeoTitle = SeoTitleFactory.Create("Updated SEO title");

        var result = product.Update(
            name: default,
            seoTitle: newSeoTitle,
            fullDescription: default,
            metaDescription: default,
            categoryId: default,
            price: default,
            isFeatured: default,
            availability: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        product.SeoTitle.Should().Be(newSeoTitle);
    }

    [Fact]
    public void Update_ShouldSucceed_ClearingOptionalSeoTitle()
    {
        var product = ProductFactory.Create(TestTimestamp, seoTitle: "Test SEO title");

        var result = product.Update(
            name: default,
            seoTitle: Optional<SeoTitle?>.FromValue(null),
            fullDescription: default,
            metaDescription: default,
            categoryId: default,
            price: default,
            isFeatured: default,
            availability: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        product.SeoTitle.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingMultipleFields()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var newName = ProductNameFactory.Create("Updated product");
        var newPrice = MoneyFactory.Create(149.99m);

        var result = product.Update(
            name: newName,
            seoTitle: default,
            fullDescription: default,
            metaDescription: default,
            categoryId: default,
            price: newPrice,
            isFeatured: true,
            availability: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        product.Name.Should().Be(newName);
        product.Price.Should().Be(newPrice);
        product.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldReturnFalse_WhenNoFieldsSet()
    {
        var product = ProductFactory.Create(TestTimestamp);

        var result = product.Update(
            name: default,
            seoTitle: default,
            fullDescription: default,
            metaDescription: default,
            categoryId: default,
            price: default,
            isFeatured: default,
            availability: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldReturnFalse_WhenFieldsUnchanged()
    {
        var product = ProductFactory.Create(TestTimestamp);

        var result = product.Update(
            name: product.Name,
            seoTitle: default,
            fullDescription: default,
            metaDescription: default,
            categoryId: default,
            price: default,
            isFeatured: product.IsFeatured,
            availability: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_ShouldFail_ForInvalidCategoryId(long categoryId)
    {
        var product = ProductFactory.Create(TestTimestamp);

        var result = product.Update(
            name: default,
            seoTitle: default,
            fullDescription: default,
            metaDescription: default,
            categoryId: categoryId,
            price: default,
            isFeatured: default,
            availability: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<ProductErrors.CategoryIdInvalid>();
        error.AttemptedId.Should().Be(categoryId);
    }

    [Fact]
    public void Update_ShouldFail_ForInvalidPublishStatusTransition()
    {
        var currentStatus = PublishStatus.Published;
        var attemptedStatus = PublishStatus.Draft;
        var product = ProductFactory.Create(TestTimestamp, publishStatus: currentStatus);

        var result = product.Update(
            name: default,
            seoTitle: default,
            fullDescription: default,
            metaDescription: default,
            categoryId: default,
            price: default,
            isFeatured: default,
            availability: default,
            publishStatus: attemptedStatus,
            TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<ProductErrors.PublishStatusTransitionNotAllowed>();
        error.CurrentStatus.Should().Be(currentStatus);
        error.AttemptedStatus.Should().Be(attemptedStatus);
    }

    [Fact]
    public void ChangeSlug_ShouldSucceed_AndReturnTrue()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var newSlug = SlugFactory.Create("new-product-slug");

        var result = product.ChangeSlug(newSlug, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        product.CurrentSlug.Should().Be(newSlug);
        product.UpdatedAt.Should().Be(TestTimestamp.AddHours(1));
    }

    [Fact]
    public void ChangeSlug_ShouldReturnFalse_WhenSlugUnchanged()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var sameSlug = product.CurrentSlug;

        var result = product.ChangeSlug(sameSlug, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void UpdateImages_ShouldSucceed_ReplacingImages()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var newImages = new[] { ProductImageFactory.Create(isPrimary: true) };

        var result = product.UpdateImages(newImages, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        product.Images.Should().BeEquivalentTo(newImages);
    }

    [Fact]
    public void UpdateImages_ShouldSucceed_ClearingImages()
    {
        var initialImages = new[] { ProductImageFactory.Create(isPrimary: true), };
        var product = ProductFactory.Create(TestTimestamp, images: initialImages);

        var result = product.UpdateImages([], TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        product.Images.Should().BeEmpty();
    }

    [Fact]
    public void UpdateImages_ShouldFail_WhenNoPrimaryImage()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var images = new[]
        {
            ProductImageFactory.Create(isPrimary: false)
        };

        var result = product.UpdateImages(images, TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        result.Should().HaveSingleError<ProductErrors.NoPrimaryImage>();
    }

    [Fact]
    public void UpdateImages_ShouldFail_WhenMultiplePrimaryImages()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var images = new[]
        {
            ProductImageFactory.Create("/images/img1.jpg", "Image 1", isPrimary: true, displayOrder: 1),
            ProductImageFactory.Create("/images/img2.jpg", "Image 2", isPrimary: true, displayOrder: 2)
        };

        var result = product.UpdateImages(images, TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<ProductErrors.MultiplePrimaryImages>();
        error.PrimaryIndices.Should().BeEquivalentTo([0, 1]);
    }

    [Fact]
    public void UpdateImages_ShouldFail_WhenTooManyImages()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var images = Enumerable.Range(1, 21)
            .Select(i => ProductImageFactory.Create(
                $"/images/img{i}.jpg",
                $"Image {i}",
                isPrimary: i == 1,
                displayOrder: i))
            .ToArray();

        var result = product.UpdateImages(images, TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<ProductErrors.TooManyImages>();
        error.AttemptedCount.Should().Be(21);
    }

    [Fact]
    public void Archive_ShouldSucceed_AndSetDeletedAt()
    {
        var product = ProductFactory.Create(TestTimestamp);
        var archiveTime = TestTimestamp.AddHours(1);

        var result = product.Archive(archiveTime);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        product.DeletedAt.Should().Be(archiveTime);
        product.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Archive_ShouldReturnFalse_WhenAlreadyArchived()
    {
        var product = ProductFactory.Create(TestTimestamp);
        product.Archive(TestTimestamp.AddHours(1));

        var result = product.Archive(TestTimestamp.AddHours(2));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        product.DeletedAt.Should().Be(TestTimestamp.AddHours(1)); // Unchanged
    }

    [Fact]
    public void Restore_ShouldSucceed_AndClearDeletedAt()
    {
        var product = ProductFactory.Create(TestTimestamp);
        product.Archive(TestTimestamp.AddHours(1));

        var result = product.Restore();

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        product.DeletedAt.Should().BeNull();
        product.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_ShouldReturnFalse_WhenNotArchived()
    {
        var product = ProductFactory.Create(TestTimestamp);

        var result = product.Restore();

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        product.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void IsDeleted_ShouldBeFalse_WhenDeletedAtIsNull()
    {
        var product = ProductFactory.Create(TestTimestamp);

        product.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_ShouldBeTrue_WhenDeletedAtHasValue()
    {
        var product = ProductFactory.Create(TestTimestamp);
        product.Archive(TestTimestamp.AddHours(1));

        product.IsDeleted.Should().BeTrue();
    }
}
