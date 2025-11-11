using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class UrlPathTests
{
    [Theory]
    [InlineData("products/roses")]
    [InlineData("products/roses/red-rose-bouquet")]
    [InlineData("/")]
    [InlineData("products")]
    [InlineData("/products/roses?color=red")]
    [InlineData("api/v1/products")]
    public void Create_ShouldSucceed_ForValidInput(string value)
    {
        var result = UrlPath.Create(value);

        result.Should().BeSuccess();
        var urlPath = result.Value!;
        urlPath.Value.Should().Be(value);
    }

    [Fact]
    public void Create_ShouldSucceed_ForExactlyMaxLength()
    {
        var value = new string('a', UrlPath.MaxLength);
        var result = UrlPath.Create(value);

        result.Should().BeSuccess();
        var urlPath = result.Value!;
        urlPath.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void Create_ShouldFail_ForNullOrWhitespaceInput(string? value)
    {
        var result = UrlPath.Create(value!);

        result.Should().BeFailure();
        result.Should().HaveError<UrlPathErrors.Empty>();
    }

    [Fact]
    public void Create_ShouldFail_ForTooLongInput()
    {
        var value = new string('a', UrlPath.MaxLength + 1);
        var result = UrlPath.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<UrlPathErrors.TooLong>();
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path")]
    [InlineData("ftp://example.com")]
    public void Create_ShouldFail_ForAbsoluteUri(string value)
    {
        var result = UrlPath.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<UrlPathErrors.InvalidFormat>();
    }

    [Fact]
    public void UrlPath_ShouldHaveStructuralEquality()
    {
        var sameUrlPath1 = UrlPathFactory.Create("products/roses");
        var sameUrlPath2 = UrlPathFactory.Create("products/roses");
        var differentUrlPath = UrlPathFactory.Create("products/tulips");

        sameUrlPath1.Should().Be(sameUrlPath2);
        sameUrlPath1.Should().NotBe(differentUrlPath);
        (sameUrlPath1 == sameUrlPath2).Should().BeTrue();
        (sameUrlPath1 == differentUrlPath).Should().BeFalse();
    }
}
