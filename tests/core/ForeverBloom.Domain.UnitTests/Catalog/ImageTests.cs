using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class ImageTests
{
    [Theory]
    [InlineData("/images/product.jpg")]
    [InlineData("/images/product.jpeg")]
    [InlineData("/images/product.png")]
    [InlineData("/images/product.webp")]
    [InlineData("/images/product.gif")]
    [InlineData("/images/product.avif")]
    public void Create_ShouldSucceed_ForValidExtensions(string sourcePath)
    {
        var imageResult = Image.Create(sourcePath, null);

        imageResult.Should().BeSuccess();
        var image = imageResult.Value!;
        image.Source.Value.Should().Be(sourcePath);
        image.AltText.Should().BeNull();
    }

    [Theory]
    [InlineData("/images/product.JPG")]
    [InlineData("/images/product.Jpeg")]
    [InlineData("/images/product.PNG")]
    [InlineData("/images/product.WEBP")]
    public void Create_ShouldSucceed_ForCaseInsensitiveExtensions(string sourcePath)
    {
        var imageResult = Image.Create(sourcePath, null);

        imageResult.Should().BeSuccess();
        var image = imageResult.Value!;
        image.Source.Value.Should().Be(sourcePath);
    }

    [Fact]
    public void Create_ShouldSucceed_WithNullAltText()
    {
        var imageResult = Image.Create("/images/product.jpg", null);

        imageResult.Should().BeSuccess();
        var image = imageResult.Value!;
        image.AltText.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSucceed_WithValidAltText()
    {
        var imageResult = Image.Create("/images/product.jpg", "A beautiful product");

        imageResult.Should().BeSuccess();
        var image = imageResult.Value!;
        image.AltText.Should().Be("A beautiful product");
    }

    [Fact]
    public void Create_ShouldSucceed_WithAltTextAtMaxLength()
    {
        var altText = new string('a', Image.AltTextMaxLength);
        var imageResult = Image.Create("/images/product.jpg", altText);

        imageResult.Should().BeSuccess();
        var image = imageResult.Value!;
        image.AltText.Should().Be(altText);
    }

    [Fact]
    public void Create_ShouldFail_WhenUrlPathValidationFails()
    {
        // Empty path should trigger UrlPath.Empty error
        var imageResult = Image.Create("", null);

        imageResult.Should().BeFailure();
        imageResult.Should().HaveError<UrlPathErrors.Empty>();
    }

    [Theory]
    [InlineData("/document.txt")]
    [InlineData("/file.pdf")]
    [InlineData("/image.bmp")]
    [InlineData("/file.svg")]
    [InlineData("/no-extension")]
    public void Create_ShouldFail_ForInvalidExtensions(string sourcePath)
    {
        var imageResult = Image.Create(sourcePath, null);

        imageResult.Should().BeFailure();
        imageResult.Should().HaveError<ImageErrors.InvalidExtension>();
    }

    [Fact]
    public void Create_ShouldFail_WhenAltTextExceedsMaxLength()
    {
        var altText = new string('a', Image.AltTextMaxLength + 1);
        var imageResult = Image.Create("/images/product.jpg", altText);

        imageResult.Should().BeFailure();
        imageResult.Should().HaveError<ImageErrors.AltTextTooLong>();
    }

    [Fact]
    public void Create_ShouldFailWithMultipleErrors_WhenInvalidExtensionAndAltTextTooLong()
    {
        var altText = new string('a', Image.AltTextMaxLength + 1);
        var imageResult = Image.Create("/document.txt", altText);

        imageResult.Should().BeFailure();
        imageResult.Should().HaveError<ImageErrors.InvalidExtension>();
        imageResult.Should().HaveError<ImageErrors.AltTextTooLong>();
    }

    [Fact]
    public void Image_ShouldHaveStructuralEquality_OnSourceOnly()
    {
        var sameSourceImage1 = ImageFactory.Create("/images/product1.jpg", "Red ceramic vase");
        var sameSourceImage2 = ImageFactory.Create("/images/product1.jpg", "Beautiful handcrafted vase");
        var differentSourceImage = ImageFactory.Create("/images/product2.jpg", "Red ceramic vase");

        (sameSourceImage1 == sameSourceImage2).Should().BeTrue();
        (sameSourceImage1 == differentSourceImage).Should().BeFalse();
        sameSourceImage1.GetHashCode().Should().Be(sameSourceImage1.Source.GetHashCode());
        sameSourceImage1.GetHashCode().Should().Be(sameSourceImage2.GetHashCode());
        sameSourceImage1.GetHashCode().Should().NotBe(differentSourceImage.GetHashCode());
    }
}
