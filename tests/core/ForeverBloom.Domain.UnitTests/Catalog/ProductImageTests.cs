using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class ProductImageTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidInputs()
    {
        var image = ImageFactory.Create();
        const bool isPrimary = true;
        const int displayOrder = 0;

        var productImage = ProductImage.Create(image, isPrimary, displayOrder);

        productImage.Should().NotBeNull();
        productImage.Image.Should().Be(image);
        productImage.IsPrimary.Should().Be(isPrimary);
        productImage.DisplayOrder.Should().Be(displayOrder);
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingAltTextOnly()
    {
        var productImage = ProductImageFactory.Create();
        const string newAltText = "Updated alt text";

        var result = productImage.Update(
            altText: newAltText,
            isPrimary: default,
            displayOrder: default);

        result.Should().BeSuccess();
        productImage.Image.AltText.Should().Be(newAltText);
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingIsPrimaryOnly()
    {
        var productImage = ProductImageFactory.Create();

        var result = productImage.Update(
            altText: default,
            isPrimary: true,
            displayOrder: default);

        result.Should().BeSuccess();
        productImage.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingDisplayOrderOnly()
    {
        var productImage = ProductImageFactory.Create();
        const int newDisplayOrder = 10;

        var result = productImage.Update(
            altText: default,
            isPrimary: default,
            displayOrder: newDisplayOrder);

        result.Should().BeSuccess();
        productImage.DisplayOrder.Should().Be(newDisplayOrder);
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingMultipleFields()
    {
        var productImage = ProductImageFactory.Create();
        const string newAltText = "Updated alt text";
        const int newDisplayOrder = 5;

        var result = productImage.Update(
            altText: newAltText,
            isPrimary: true,
            displayOrder: newDisplayOrder);

        result.Should().BeSuccess();
        productImage.Image.AltText.Should().Be(newAltText);
        productImage.IsPrimary.Should().BeTrue();
        productImage.DisplayOrder.Should().Be(newDisplayOrder);
    }

    [Fact]
    public void Update_ShouldSucceed_WithOptionalUnset()
    {
        var productImage = ProductImageFactory.Create();
        var originalImage = productImage.Image;
        var originalIsPrimary = productImage.IsPrimary;
        var originalDisplayOrder = productImage.DisplayOrder;

        var result = productImage.Update(
            altText: default,
            isPrimary: default,
            displayOrder: default);

        result.Should().BeSuccess();
        productImage.Image.Should().Be(originalImage);
        productImage.IsPrimary.Should().Be(originalIsPrimary);
        productImage.DisplayOrder.Should().Be(originalDisplayOrder);
    }

    [Fact]
    public void Update_ShouldSucceed_ClearingAltText()
    {
        var productImage = ProductImageFactory.Create();

        var result = productImage.Update(
            altText: Optional<string?>.FromValue(null),
            isPrimary: default,
            displayOrder: default);

        result.Should().BeSuccess();
        productImage.Image.AltText.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldFail_ForTooLongAltText()
    {
        var productImage = ProductImageFactory.Create();
        var tooLongAltText = new string('a', Image.AltTextMaxLength + 1);

        var result = productImage.Update(
            altText: tooLongAltText,
            isPrimary: default,
            displayOrder: default);

        result.Should().BeFailure();
        result.Should().HaveError<ImageErrors.AltTextTooLong>();
    }
}
