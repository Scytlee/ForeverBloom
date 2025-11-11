using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class ProductNameTests
{
    [Theory]
    [InlineData("Red Rose Bouquet")]
    [InlineData("A")]
    [InlineData("Product 123")]
    [InlineData("Name with special chars: !@#$%")]
    public void Create_ShouldSucceed_ForValidInput(string value)
    {
        var result = ProductName.Create(value);

        result.Should().BeSuccess();
        var productName = result.Value!;
        productName.Value.Should().Be(value);
    }

    [Fact]
    public void Create_ShouldSucceed_ForExactlyMaxLength()
    {
        var value = new string('a', ProductName.MaxLength);
        var result = ProductName.Create(value);

        result.Should().BeSuccess();
        var productName = result.Value!;
        productName.Value.Should().Be(value);
    }

    [Fact]
    public void Create_ShouldSucceed_AndTrimInput()
    {
        var result = ProductName.Create("  Trimmed Product  ");

        result.Should().BeSuccess();
        var productName = result.Value!;
        productName.Value.Should().Be("Trimmed Product");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void Create_ShouldFail_ForNullOrWhitespaceInput(string? value)
    {
        var result = ProductName.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<ProductNameErrors.Required>();
    }

    [Fact]
    public void Create_ShouldFail_ForTooLongInput()
    {
        var value = new string('a', ProductName.MaxLength + 1);
        var result = ProductName.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<ProductNameErrors.TooLong>();
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var result = ProductName.Create("Test Product");
        var productName = result.Value!;

        productName.ToString().Should().Be("Test Product");
    }

    [Fact]
    public void ProductName_ShouldHaveStructuralEquality()
    {
        var sameProductName1 = ProductNameFactory.Create("Same name");
        var sameProductName2 = ProductNameFactory.Create("Same name");
        var differentProductName = ProductNameFactory.Create("Different name");

        sameProductName1.Should().Be(sameProductName2);
        sameProductName1.Should().NotBe(differentProductName);
        (sameProductName1 == sameProductName2).Should().BeTrue();
        (sameProductName1 == differentProductName).Should().BeFalse();
    }
}
