using FluentAssertions;
using ForeverBloom.Application.Sorting;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Sorting;

public sealed class SortPropertyParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Parse_ShouldReturnEmptyArray_WhenInputIsNullOrWhitespace(string? input)
    {
        var result = SortPropertyParser.Parse(input);

        result.Should().BeSuccess();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldParseSingleProperty()
    {
        var result = SortPropertyParser.Parse("name:asc");

        result.Should().BeSuccess();
        result.Value.Should().BeEquivalentTo([
            new SortProperty("name", SortDirection.Ascending)
        ]);
    }

    [Fact]
    public void Parse_ShouldParseMultipleProperties()
    {
        var result = SortPropertyParser.Parse("name:asc,price:desc,created_at:asc");

        result.Should().BeSuccess();
        result.Value.Should().BeEquivalentTo([
            new SortProperty("name", SortDirection.Ascending),
            new SortProperty("price", SortDirection.Descending),
            new SortProperty("created_at", SortDirection.Ascending)
        ]);
    }

    [Theory]
    [InlineData("name:ASC", SortDirection.Ascending)]
    [InlineData("name:Desc", SortDirection.Descending)]
    public void Parse_ShouldBeCaseInsensitiveForDirection(string input, SortDirection expectedDirection)
    {
        var result = SortPropertyParser.Parse(input);

        result.Should().BeSuccess();
        result.Value!.First().Direction.Should().Be(expectedDirection);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("name:asc:extra")]
    public void Parse_ShouldReturnFailure_WhenFormatIsInvalid(string input)
    {
        var result = SortPropertyParser.Parse(input);

        result.Should().BeFailure();
        result.Should().HaveError<SortingErrors.InvalidSortFormat>();
    }

    [Theory]
    [InlineData("name:invalid")]
    [InlineData("name:")]
    public void Parse_ShouldReturnFailure_WhenDirectionIsInvalid(string input)
    {
        var result = SortPropertyParser.Parse(input);

        result.Should().BeFailure();
        result.Should().HaveError<SortingErrors.InvalidSortDirection>();
    }

    [Fact]
    public void Parse_ShouldCollectMultipleErrors()
    {
        var result = SortPropertyParser.Parse("name:invalid,price,category:wrong");

        result.Should().BeFailure();
        result.Should().HaveError<SortingErrors.InvalidSortFormat>();
        result.Should().HaveError<SortingErrors.InvalidSortDirection>();
    }
}
