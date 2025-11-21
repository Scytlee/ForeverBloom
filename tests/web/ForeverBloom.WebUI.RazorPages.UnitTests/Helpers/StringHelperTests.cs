using FluentAssertions;
using ForeverBloom.WebUI.RazorPages.Helpers;

namespace ForeverBloom.WebUI.RazorPages.UnitTests.Helpers;

public sealed class StringHelperTests
{
    [Fact]
    public void Truncate_ShouldReturnEmptyString_WhenValueIsNull()
    {
        string? value = null;

        var result = value.Truncate(10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Truncate_ShouldReturnEmptyString_WhenValueIsEmpty()
    {
        var value = string.Empty;

        var result = value.Truncate(10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Truncate_ShouldReturnEmptyString_WhenValueIsWhitespace()
    {
        var result = "   ".Truncate(10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Truncate_ShouldReturnOriginalValue_WhenShorterThanMaxLength()
    {
        var result = "Hello".Truncate(10);

        result.Should().Be("Hello");
    }

    [Fact]
    public void Truncate_ShouldReturnOriginalValue_WhenExactlyMaxLength()
    {
        var result = "HelloWorld".Truncate(10);

        result.Should().Be("HelloWorld");
    }

    [Fact]
    public void Truncate_ShouldTruncateWithDefaultSuffix_WhenLongerThanMaxLength()
    {
        var result = "This is a very long string".Truncate(10);

        result.Should().Be("This is...");
        result.Length.Should().Be(10);
    }

    [Fact]
    public void Truncate_ShouldTruncateWithCustomSuffix_WhenProvided()
    {
        var result = "This is a very long string".Truncate(15, " (more)");

        result.Should().Be("This is  (more)");
        result.Length.Should().Be(15);
    }
}
