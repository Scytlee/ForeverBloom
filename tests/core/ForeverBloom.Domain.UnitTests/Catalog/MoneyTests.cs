using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class MoneyTests
{
    [Theory]
    [InlineData(10.00)]
    [InlineData(1.50)]
    [InlineData(123.45)]
    [InlineData(10.1)]
    [InlineData(10)]
    [InlineData(5.5)]
    [InlineData(10.250)]
    [InlineData(10.26000000)]
    [InlineData(0.00)]
    [InlineData(0)]
    public void Create_ShouldSucceed_ForValidInput(decimal value)
    {
        var result = Money.Create(value);

        result.Should().BeSuccess();
        var money = result.Value!;
        money.Value.Should().Be(value);
    }

    [Fact]
    public void Create_ShouldSucceed_ForZero()
    {
        var result = Money.Create(0.00m);

        result.Should().BeSuccess();
        var money = result.Value!;
        money.Value.Should().Be(0.00m);
    }

    [Fact]
    public void Create_ShouldSucceed_ForSmallestPositiveValue()
    {
        var result = Money.Create(0.01m);

        result.Should().BeSuccess();
        var money = result.Value!;
        money.Value.Should().Be(0.01m);
    }

    [Fact]
    public void Create_ShouldSucceed_ForLargeValue()
    {
        var result = Money.Create(99999999.99m);

        result.Should().BeSuccess();
        var money = result.Value!;
        money.Value.Should().Be(99999999.99m);
    }

    [Theory]
    [InlineData(-5.00)]
    [InlineData(-0.01)]
    [InlineData(-100.00)]
    public void Create_ShouldFail_ForNegativeValue(decimal value)
    {
        var result = Money.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<MoneyErrors.Negative>();
    }

    [Theory]
    [InlineData(10.123)]
    [InlineData(10.1234)]
    [InlineData(5.999)]
    [InlineData(1.001)]
    [InlineData(0.999)]
    public void Create_ShouldFail_ForValueNotRepresentableWithTwoDecimalPlaces(decimal value)
    {
        var result = Money.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<MoneyErrors.InvalidPrecision>();
    }

    [Fact]
    public void Create_ShouldFailWithMultipleErrors_WhenNegativeAndNotRepresentableWithTwoDecimalPlaces()
    {
        var result = Money.Create(-0.123m);

        result.Should().BeFailure();
        result.Should().HaveError<MoneyErrors.Negative>();
        result.Should().HaveError<MoneyErrors.InvalidPrecision>();
    }

    [Fact]
    public void Money_ShouldHaveStructuralEquality()
    {
        var sameMoney1 = MoneyFactory.Create(25.00m);
        var sameMoney2 = MoneyFactory.Create(25.00m);
        var differentMoney = MoneyFactory.Create(30.00m);

        sameMoney1.Should().Be(sameMoney2);
        sameMoney1.Should().NotBe(differentMoney);
        (sameMoney1 == sameMoney2).Should().BeTrue();
        (sameMoney1 == differentMoney).Should().BeFalse();
    }
}
