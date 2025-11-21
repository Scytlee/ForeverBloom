using FluentAssertions;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Products.Commands.UpdateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Optional;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;

namespace ForeverBloom.Application.UnitTests.Products;

public sealed class UpdateProductCommandTests
{
    private static Result<UpdateProductCommand> CreateCommandWith(
        long productId = 1,
        uint rowVersion = 1,
        Optional<string> name = default,
        Optional<string?> seoTitle = default,
        Optional<string?> fullDescription = default,
        Optional<string?> metaDescription = default,
        Optional<long> categoryId = default,
        Optional<decimal?> price = default,
        Optional<bool> isFeatured = default,
        Optional<string> availability = default,
        Optional<string> publishStatus = default)
    {
        return UpdateProductCommand.Create(
            productId,
            rowVersion,
            name,
            seoTitle,
            fullDescription,
            metaDescription,
            categoryId,
            price,
            isFeatured,
            availability,
            publishStatus);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValidAndOptionalParametersAreProvided()
    {
        const long productId = 10;
        const uint rowVersion = 3;
        const string name = "Radiant Peonies";
        const string seoTitle = "Radiant Peonies";
        const string fullDescription = "<p>Hand-tied peonies arrangement</p>";
        const string metaDescription = "Lush peonies bouquet";
        const long categoryId = 7;
        const decimal price = 349.99m;
        const bool isFeatured = true;
        const string availability = "available";
        const string publishStatus = "published";

        var createResult = UpdateProductCommand.Create(
            productId,
            rowVersion,
            name,
            seoTitle,
            fullDescription,
            metaDescription,
            categoryId,
            price,
            isFeatured,
            availability,
            publishStatus);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.ProductId.Should().Be(productId);
        command.RowVersion.Should().Be(rowVersion);
        command.Name.Value.Should().HaveValue(name);
        command.SeoTitle.Value.Should().HaveValue(seoTitle);
        command.FullDescription.Value.Should().HaveValue(fullDescription);
        command.MetaDescription.Value.Should().HaveValue(metaDescription);
        command.CategoryId.Should().BeSetTo(categoryId);
        command.Price.Should().BeSet();
        command.Price.Value.Should().HaveValue(price);
        command.IsFeatured.Should().BeSetTo(isFeatured);
        command.Availability.Should().BeSetTo(ProductAvailabilityStatus.Available);
        command.PublishStatus.Should().BeSetTo(PublishStatus.Published);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValidAndOptionalParametersAreOmitted()
    {
        const long productId = 5;
        const uint rowVersion = 2;

        var createResult = UpdateProductCommand.Create(productId, rowVersion);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.ProductId.Should().Be(productId);
        command.RowVersion.Should().Be(rowVersion);
        command.Name.Should().BeUnset();
        command.SeoTitle.Should().BeUnset();
        command.FullDescription.Should().BeUnset();
        command.MetaDescription.Should().BeUnset();
        command.CategoryId.Should().BeUnset();
        command.Price.Should().BeUnset();
        command.IsFeatured.Should().BeUnset();
        command.Availability.Should().BeUnset();
        command.PublishStatus.Should().BeUnset();
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenOptionalParametersAreExplicitlySetToNull()
    {
        const long productId = 8;
        const uint rowVersion = 4;

        var createResult = UpdateProductCommand.Create(
            productId,
            rowVersion,
            seoTitle: (string?)null,
            fullDescription: (string?)null,
            metaDescription: (string?)null,
            price: (decimal?)null);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.SeoTitle.Should().BeSetToNull();
        command.FullDescription.Should().BeSetToNull();
        command.MetaDescription.Should().BeSetToNull();
        command.Price.Should().BeSetToNull();
    }

    [Fact]
    public void Create_ShouldAcceptPriceWithTrailingZeros_WhenPrecisionIsEffectivelyTwoDecimalPlaces()
    {
        const decimal price = 10.9500m;

        var createResult = CreateCommandWith(
            price: price);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.Price.Should().BeSet();
        command.Price.Value.Should().HaveValue(10.95m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFailure_WhenProductIdIsInvalid(long invalidProductId)
    {
        var createResult = CreateCommandWith(productId: invalidProductId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.ProductIdInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenRowVersionIsInvalid()
    {
        var createResult = CreateCommandWith(rowVersion: 0);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ApplicationErrors.RowVersionInvalid>();
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
    public void Create_ShouldReturnFailure_WhenSeoTitleIsInvalid()
    {
        var invalidSeoTitle = new string('a', SeoTitle.MaxLength + 1);

        var createResult = CreateCommandWith(seoTitle: invalidSeoTitle);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SeoTitleErrors.TooLong>();
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
    public void Create_ShouldReturnFailure_WhenMetaDescriptionIsInvalid()
    {
        var invalidMetaDescription = new string('a', MetaDescription.MaxLength + 1);

        var createResult = CreateCommandWith(metaDescription: invalidMetaDescription);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<MetaDescriptionErrors.TooLong>();
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
    public void Create_ShouldReturnFailure_WhenPriceIsNegative()
    {
        const decimal negativePrice = -10.00m;

        var createResult = CreateCommandWith(price: negativePrice);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<MoneyErrors.Negative>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenPriceHasTooManyDecimalPlaces()
    {
        const decimal invalidPrecisionPrice = 10.123m;

        var createResult = CreateCommandWith(price: invalidPrecisionPrice);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<MoneyErrors.InvalidPrecision>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not_real")]
    public void Create_ShouldReturnFailure_WhenAvailabilityStatusIsInvalid(string invalidAvailability)
    {
        var createResult = CreateCommandWith(availability: invalidAvailability);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductAvailabilityStatusErrors.InvalidName>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("unknown_status")]
    public void Create_ShouldReturnFailure_WhenPublishStatusIsInvalid(string invalidPublishStatus)
    {
        var createResult = CreateCommandWith(publishStatus: invalidPublishStatus);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PublishStatusErrors.InvalidName>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const long invalidProductId = 0;
        var invalidName = new string('a', ProductName.MaxLength + 1);

        var createResult = CreateCommandWith(
            productId: invalidProductId,
            name: invalidName);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.ProductIdInvalid>();
        createResult.Should().HaveError<ProductNameErrors.TooLong>();
    }
}
