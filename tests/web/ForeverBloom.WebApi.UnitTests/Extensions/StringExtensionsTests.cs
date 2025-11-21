using FluentAssertions;
using ForeverBloom.WebApi.Extensions;

namespace ForeverBloom.WebApi.UnitTests.Extensions;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("PropertyName", "propertyName")]
    [InlineData("Name", "name")]
    [InlineData("ID", "iD")]
    [InlineData("HTTPRequest", "hTTPRequest")]
    [InlineData("A", "a")]
    [InlineData("PropertyNameValue", "propertyNameValue")]
    public void ToCamelCase_ShouldConvertFirstCharacterToLowerCase_WhenGivenPascalCaseString(
        string input,
        string expected)
    {
        var result = input.ToCamelCase();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("alreadyCamelCase")]
    [InlineData("a")]
    [InlineData("propertyOne")]
    [InlineData("id")]
    public void ToCamelCase_ShouldReturnUnchanged_WhenGivenCamelCaseString(string input)
    {
        var result = input.ToCamelCase();

        result.Should().Be(input);
    }

    [Theory]
    [InlineData("123Property")]
    [InlineData("_Property")]
    public void ToCamelCase_ShouldReturnUnchanged_WhenStringStartsWithNonLetter(string input)
    {
        var result = input.ToCamelCase();

        result.Should().Be(input);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToCamelCase_ShouldReturnOriginal_WhenGivenNullOrWhitespace(string? input)
    {
        var result = input!.ToCamelCase();

        result.Should().Be(input);
    }
}
