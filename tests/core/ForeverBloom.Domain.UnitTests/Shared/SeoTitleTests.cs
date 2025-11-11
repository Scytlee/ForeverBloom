using FluentAssertions;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Shared;

public sealed class SeoTitleTests
{
    [Theory]
    [InlineData("Elegant Rose Bouquet")]
    [InlineData("A")]
    [InlineData("Product 123!")]
    [InlineData("Title with numbers 123 and symbols!?")]
    public void Create_ShouldSucceed_ForValidInput(string value)
    {
        var result = SeoTitle.Create(value);

        result.Should().BeSuccess();
        var seoTitle = result.Value!;
        seoTitle.Value.Should().Be(value);
    }

    [Fact]
    public void Create_ShouldSucceed_ForExactlyMaxLength()
    {
        var value = new string('a', SeoTitle.MaxLength);
        var result = SeoTitle.Create(value);

        result.Should().BeSuccess();
        var seoTitle = result.Value!;
        seoTitle.Value.Should().Be(value);
    }

    [Fact]
    public void Create_ShouldSucceed_AndTrimInput()
    {
        var result = SeoTitle.Create("  Trimmed Title  ");

        result.Should().BeSuccess();
        var seoTitle = result.Value!;
        seoTitle.Value.Should().Be("Trimmed Title");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void Create_ShouldFail_ForNullOrWhitespaceInput(string? value)
    {
        var result = SeoTitle.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<SeoTitleErrors.Empty>();
    }

    [Fact]
    public void Create_ShouldFail_ForTooLongInput()
    {
        var value = new string('a', SeoTitle.MaxLength + 1);
        var result = SeoTitle.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<SeoTitleErrors.TooLong>();
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var result = SeoTitle.Create("Test Title");
        var seoTitle = result.Value!;

        seoTitle.ToString().Should().Be("Test Title");
    }

    [Fact]
    public void SeoTitle_ShouldHaveStructuralEquality()
    {
        var sameSeoTitle1 = SeoTitleFactory.Create("Same title");
        var sameSeoTitle2 = SeoTitleFactory.Create("Same title");
        var differentSeoTitle = SeoTitleFactory.Create("Different title");

        sameSeoTitle1.Should().Be(sameSeoTitle2);
        sameSeoTitle1.Should().NotBe(differentSeoTitle);
        (sameSeoTitle1 == sameSeoTitle2).Should().BeTrue();
        (sameSeoTitle1 == differentSeoTitle).Should().BeFalse();
    }
}
