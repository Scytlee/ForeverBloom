using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class HtmlFragmentTests
{
    [Theory]
    [InlineData("<p>Simple paragraph</p>")]
    [InlineData("<strong>Bold text</strong>")]
    [InlineData("<em>Italic text</em>")]
    [InlineData("<a href=\"https://example.com\">Link</a>")]
    [InlineData("<div><p>Nested</p></div>")]
    [InlineData("<ul><li>Item 1</li><li>Item 2</li></ul>")]
    [InlineData("<p>Text with <strong>bold</strong> and <em>italic</em></p>")]
    public void Create_ShouldSucceed_ForValidHtml(string html)
    {
        var result = HtmlFragment.Create(html);

        result.Should().BeSuccess();
        var htmlFragment = result.Value!;
        htmlFragment.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void Create_ShouldFail_ForNullOrWhitespaceInput(string? value)
    {
        var result = HtmlFragment.Create(value!);

        result.Should().BeFailure();
        result.Should().HaveError<HtmlFragmentErrors.Empty>();
    }

    [Fact]
    public void Create_ShouldFail_ForTooLongInput()
    {
        var tooLongHtml = $"<p>{new string('a', HtmlFragment.MaxLength)}</p>";

        var result = HtmlFragment.Create(tooLongHtml);

        result.Should().BeFailure();
        result.Should().HaveError<HtmlFragmentErrors.TooLong>();
    }

    [Fact]
    public void Create_ShouldSucceed_ForHtmlAtMaxLength()
    {
        var htmlAtMaxLength = $"<p>{new string('a', HtmlFragment.MaxLength - 7)}</p>";

        var result = HtmlFragment.Create(htmlAtMaxLength);

        result.Should().BeSuccess();
    }

    [Theory]
    [InlineData("<div><p>Unclosed div")]
    [InlineData("<")]
    [InlineData("<p")]
    public void Create_ShouldFail_ForMalformedHtml(string html)
    {
        var result = HtmlFragment.Create(html);

        result.Should().BeFailure();
        result.Should().HaveError<HtmlFragmentErrors.Malformed>();
    }

    [Fact]
    public void Create_ShouldFailWithMultipleErrors_ForTooLongAndMalformed()
    {
        var tooLongMalformedHtml = $"<div>{new string('a', HtmlFragment.MaxLength)}";

        var result = HtmlFragment.Create(tooLongMalformedHtml);

        result.Should().BeFailure();
        result.Should().HaveError<HtmlFragmentErrors.TooLong>();
        result.Should().HaveError<HtmlFragmentErrors.Malformed>();
    }

    [Fact]
    public void HtmlFragment_ShouldHaveStructuralEquality()
    {
        var sameHtmlFragment1 = HtmlFragmentFactory.Create("<p>Test paragraph</p>");
        var sameHtmlFragment2 = HtmlFragmentFactory.Create("<p>Test paragraph</p>");
        var differentHtmlFragment = HtmlFragmentFactory.Create("<p>Different paragraph</p>");

        sameHtmlFragment1.Should().Be(sameHtmlFragment2);
        sameHtmlFragment1.Should().NotBe(differentHtmlFragment);
        (sameHtmlFragment1 == sameHtmlFragment2).Should().BeTrue();
        (sameHtmlFragment1 == differentHtmlFragment).Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSucceed_ForPlainText()
    {
        var result = HtmlFragment.Create("Plain text without tags");

        result.Should().BeSuccess();
    }
}
