using FluentAssertions;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;

namespace ForeverBloom.Application.UnitTests.Products;

public sealed class CreateProductCommandTests
{
    private static Result<CreateProductCommand> CreateCommandWith(
        string name = "Test Product",
        string slug = "test-product",
        long categoryId = 1,
        bool isFeatured = false,
        string availabilityStatus = "coming_soon",
        string? seoTitle = null,
        string? fullDescription = null,
        string? metaDescription = null,
        decimal? price = null,
        CreateProductCommandImage[]? images = null)
    {
        return CreateProductCommand.Create(
            name,
            slug,
            categoryId,
            isFeatured,
            availabilityStatus,
            seoTitle,
            fullDescription,
            metaDescription,
            price,
            images);
    }

    private static CreateProductCommandImage CreateImage(
        string source = "/images/product.jpg",
        string? altText = "Product image",
        bool isPrimary = false,
        int displayOrder = 0)
    {
        return new CreateProductCommandImage(source, altText, isPrimary, displayOrder);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValidAndOptionalParametersAreProvided()
    {
        const string name = "Opulent Orchid";
        const string slug = "opulent-orchid";
        const long categoryId = 42;
        const bool isFeatured = true;
        const string availabilityStatus = "available";
        const string seoTitle = "Opulent Orchid";
        const string fullDescription = "<p>Intricate arrangement</p>";
        const string metaDescription = "Elegant orchids, ready to gift.";
        const decimal price = 249.99m;
        var images =
            new[]
            {
                CreateImage("/images/orchid-primary.jpg", "Primary orchid", true, 1),
                CreateImage("/images/orchid-secondary.webp", "Secondary orchid", false, 2)
            };

        var createResult = CreateProductCommand.Create(
            name,
            slug,
            categoryId,
            isFeatured,
            availabilityStatus,
            seoTitle,
            fullDescription,
            metaDescription,
            price,
            images);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.Name.Should().HaveValue(name);
        command.Slug.Should().HaveValue(slug);
        command.CategoryId.Should().Be(categoryId);
        command.IsFeatured.Should().BeTrue();
        command.AvailabilityStatus.Should().Be(ProductAvailabilityStatus.Available);
        command.SeoTitle.Should().HaveValue(seoTitle);
        command.FullDescription.Should().HaveValue(fullDescription);
        command.MetaDescription.Should().HaveValue(metaDescription);
        command.Price.Should().HaveValue(price);
        command.Images.Should().HaveCount(2);
        var primaryImage = command.Images[0];
        primaryImage.Image.Should().Match(images[0].Source, images[0].AltText);
        primaryImage.IsPrimary.Should().BeTrue();
        primaryImage.DisplayOrder.Should().Be(1);
        var secondaryImage = command.Images[1];
        secondaryImage.Image.Should().Match(images[1].Source, images[1].AltText);
        secondaryImage.IsPrimary.Should().BeFalse();
        secondaryImage.DisplayOrder.Should().Be(2);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenOnlyRequiredInputIsProvided()
    {
        const string name = "Everyday Bouquet";
        const string slug = "everyday-bouquet";
        const long categoryId = 5;

        var createResult = CreateCommandWith(
            name: name,
            slug: slug,
            categoryId: categoryId);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.Name.Should().HaveValue(name);
        command.Slug.Should().HaveValue(slug);
        command.CategoryId.Should().Be(categoryId);
        command.IsFeatured.Should().BeFalse();
        command.AvailabilityStatus.Should().Be(ProductAvailabilityStatus.ComingSoon);
        command.SeoTitle.Should().BeNull();
        command.FullDescription.Should().BeNull();
        command.MetaDescription.Should().BeNull();
        command.Price.Should().BeNull();
        command.Images.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenNameIsInvalid()
    {
        var invalidName = new string('a', ProductName.MaxLength + 1);

        var createResult = CreateCommandWith(name: invalidName);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductNameErrors.TooLong>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSlugIsInvalid()
    {
        const string invalidSlug = "Invalid Slug";

        var createResult = CreateCommandWith(slug: invalidSlug);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_ShouldReturnFailure_WhenCategoryIdIsInvalid(long invalidCategoryId)
    {
        var createResult = CreateCommandWith(categoryId: invalidCategoryId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.CategoryIdInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenAvailabilityStatusIsInvalid()
    {
        const string invalidStatus = "not_real";

        var createResult = CreateCommandWith(availabilityStatus: invalidStatus);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductAvailabilityStatusErrors.InvalidName>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSeoTitleIsInvalid()
    {
        var invalidSeoTitle = new string('a', SeoTitle.MaxLength + 1);

        var createResult = CreateCommandWith(seoTitle: invalidSeoTitle);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SeoTitleErrors.TooLong>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenMetaDescriptionIsInvalid()
    {
        var invalidMetaDescription = new string('a', MetaDescription.MaxLength + 1);

        var createResult = CreateCommandWith(metaDescription: invalidMetaDescription);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<MetaDescriptionErrors.TooLong>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenFullDescriptionIsInvalid()
    {
        var invalidFullDescription = new string('a', HtmlFragment.MaxLength + 1);

        var createResult = CreateCommandWith(fullDescription: invalidFullDescription);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<HtmlFragmentErrors.TooLong>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenPriceIsNegative()
    {
        var createResult = CreateCommandWith(price: -0.01m);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<MoneyErrors.Negative>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenPriceHasTooManyDecimalPlaces()
    {
        var createResult = CreateCommandWith(price: 10.999m);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<MoneyErrors.InvalidPrecision>();
    }

    [Fact]
    public void Create_ShouldReturnSuccess_WhenPriceIsRepresentableDespiteTooManyDecimalPlaces()
    {
        var createResult = CreateCommandWith(price: 10.9500m);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.Price.Should().HaveValue(10.95m);
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenImageSourceIsInvalid()
    {
        var images = new[]
        {
            CreateImage("/images/invalid-path", "Alt", true, 1)
        };

        var createResult = CreateCommandWith(images: images);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ImageErrors.InvalidExtension>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenImageAltTextIsTooLong()
    {
        var longAltText = new string('a', Image.AltTextMaxLength + 1);
        var images = new[]
        {
            CreateImage("/images/product.jpg", longAltText, true, 1)
        };

        var createResult = CreateCommandWith(images: images);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ImageErrors.AltTextTooLong>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        var invalidName = string.Empty;
        var invalidSlug = "Invalid Slug";

        var createResult = CreateCommandWith(
            name: invalidName,
            slug: invalidSlug);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductNameErrors.Required>();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }
}
