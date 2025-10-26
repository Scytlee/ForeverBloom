using System.Linq.Expressions;
using FluentAssertions;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Api.Models;

namespace ForeverBloom.Api.Tests.Helpers;

public sealed class SortingHelperTests
{
    private record TestRecord(string Name, string Category);

    private static readonly HashSet<string> AllowedColumns = SortingHelper.CreateAllowedSortColumns(
        "Name",
        "Price",
        "CategoryName");

    [Fact]
    public void CreateAllowedSortColumns_ShouldCreateCaseInsensitiveHashSet()
    {
        var columnSet = SortingHelper.CreateAllowedSortColumns("Name", "CategoryName");

        columnSet.Should().Contain("name");
        columnSet.Should().Contain("NAME");
        columnSet.Should().Contain("Name");
        columnSet.Should().Contain("categoryname");
        columnSet.Should().Contain("CATEGORYNAME");
        columnSet.Should().Contain("CategoryName");
    }

    [Fact]
    public void CreatePropertyMapping_ShouldHandleEmptyInput()
    {
        var mapping = SortingHelper.CreatePropertyMapping<TestRecord>();

        mapping.Should().BeEmpty();
    }

    [Fact]
    public void CreatePropertyMapping_ShouldCreateCaseInsensitiveDictionary()
    {
        var mapping = SortingHelper.CreatePropertyMapping<TestRecord>(
            ("name", x => x.Name),
            ("CategoryName", x => x.Category)
        );

        mapping.Should().ContainKey("name");
        mapping.Should().ContainKey("NAME");
        mapping.Should().ContainKey("Name");
        mapping.Should().ContainKey("categoryname");
        mapping.Should().ContainKey("CATEGORYNAME");
        mapping.Should().ContainKey("CategoryName");
    }

    [Fact]
    public void CreatePropertyMapping_ShouldPreserveExpressions()
    {
        var mapping = SortingHelper.CreatePropertyMapping<TestRecord>(
            ("TestField", x => x.Name.ToUpper())
        );

        var expression = mapping["testfield"]; // Case-insensitive access
        expression.Should().NotBeNull();
        expression.Parameters.Should().HaveCount(1);

        // Verify the expression body represents x => x.Name.ToUpper()
        var body = expression.Body;
        body.Should().BeAssignableTo<MethodCallExpression>();
        var methodCall = (MethodCallExpression)body;
        methodCall.Method.Name.Should().Be("ToUpper");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_ShouldAllowNullOrWhitespace(string? sortString)
    {
        var success = SortingHelper.TryParseAndValidateSortString(sortString, AllowedColumns, out var result);

        success.Should().BeTrue();
        result.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_ShouldCanonicalizePropertyName()
    {
        var success = SortingHelper.TryParseAndValidateSortString("name desc", AllowedColumns, out var result);

        success.Should().BeTrue();
        result.Should().ContainSingle()
            .Which.Should().Be(new SortCriterion("Name", "desc"));
    }

    [Fact]
    public void TryParse_ShouldDefaultSortsToAscending()
    {
        var success = SortingHelper.TryParseAndValidateSortString("Name", AllowedColumns, out var result);

        success.Should().BeTrue();
        result.Should().ContainSingle()
            .Which.Should().Be(new SortCriterion("Name", "asc"));
    }

    [Theory]
    [InlineData("Price Desc")]
    [InlineData("price desc")]
    [InlineData("PRICE DESC")]
    public void TryParse_ShouldHandleMixedCasing(string sortString)
    {
        var success = SortingHelper.TryParseAndValidateSortString(sortString, AllowedColumns, out var result);

        success.Should().BeTrue();
        result.Should().ContainSingle()
            .Which.Should().Be(new SortCriterion("Price", "desc"));
    }

    [Fact]
    public void TryParse_ShouldHandleMultipleColumns()
    {
        var success = SortingHelper.TryParseAndValidateSortString("Name asc, CategoryName desc", AllowedColumns, out var result);

        success.Should().BeTrue();
        result.Should().HaveCount(2);
        result[0].Should().Be(new SortCriterion("Name", "asc"));
        result[1].Should().Be(new SortCriterion("CategoryName", "desc"));
    }

    [Fact]
    public void TryParse_ShouldRejectDuplicateProperties()
    {
        var success = SortingHelper.TryParseAndValidateSortString("Price asc, Price desc", AllowedColumns, out var result);

        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_ShouldRejectUnknownProperties()
    {
        var success = SortingHelper.TryParseAndValidateSortString("Height desc", AllowedColumns, out _);

        success.Should().BeFalse();
    }

    [Theory]
    [InlineData("Name asc Price desc")]
    [InlineData("Name a sc, Price desc")]
    [InlineData("Name asc,")]
    [InlineData(",Name asc")]
    public void TryParse_ShouldRejectMalformedString(string sortString)
    {
        var success = SortingHelper.TryParseAndValidateSortString(sortString, AllowedColumns, out _);

        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_ShouldRejectInvalidDirection()
    {
        var success = SortingHelper.TryParseAndValidateSortString("Name sideways", AllowedColumns, out _);

        success.Should().BeFalse();
    }
}
