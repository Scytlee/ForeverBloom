using System.Diagnostics;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class ProductAvailabilityStatusTests
{
    [Theory]
    [InlineData(1, nameof(ProductAvailabilityStatus.Available))]
    [InlineData(2, nameof(ProductAvailabilityStatus.OutOfStock))]
    [InlineData(3, nameof(ProductAvailabilityStatus.MadeToOrder))]
    [InlineData(5, nameof(ProductAvailabilityStatus.Discontinued))]
    [InlineData(6, nameof(ProductAvailabilityStatus.ComingSoon))]
    public void FromCode_ShouldReturnCorrectStatus_ForValidCodes(int code, string expectedStatusName)
    {
        var result = ProductAvailabilityStatus.FromCode(code);

        result.Should().BeSuccess();
        var status = result.Value!;
        status.Code.Should().Be(code);

        var expectedStatus = expectedStatusName switch
        {
            nameof(ProductAvailabilityStatus.Available) => ProductAvailabilityStatus.Available,
            nameof(ProductAvailabilityStatus.OutOfStock) => ProductAvailabilityStatus.OutOfStock,
            nameof(ProductAvailabilityStatus.MadeToOrder) => ProductAvailabilityStatus.MadeToOrder,
            nameof(ProductAvailabilityStatus.Discontinued) => ProductAvailabilityStatus.Discontinued,
            nameof(ProductAvailabilityStatus.ComingSoon) => ProductAvailabilityStatus.ComingSoon,
            _ => throw new UnreachableException()
        };

        status.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(-1)]
    [InlineData(999)]
    public void FromCode_ShouldFail_ForInvalidCodes(int code)
    {
        var result = ProductAvailabilityStatus.FromCode(code);

        result.Should().BeFailure();
        result.Should().HaveError<ProductAvailabilityStatusErrors.InvalidCode>();
    }

    [Fact]
    public void All_ShouldContainAllPredefinedStatuses()
    {
        ProductAvailabilityStatus.All.Should().HaveCount(5);
        ProductAvailabilityStatus.All.Should().Contain(ProductAvailabilityStatus.Available);
        ProductAvailabilityStatus.All.Should().Contain(ProductAvailabilityStatus.OutOfStock);
        ProductAvailabilityStatus.All.Should().Contain(ProductAvailabilityStatus.MadeToOrder);
        ProductAvailabilityStatus.All.Should().Contain(ProductAvailabilityStatus.Discontinued);
        ProductAvailabilityStatus.All.Should().Contain(ProductAvailabilityStatus.ComingSoon);
    }

    [Fact]
    public void ProductAvailabilityStatus_ShouldHaveStructuralEquality()
    {
        var sameStatus1 = ProductAvailabilityStatus.Available;
        var sameStatus2 = ProductAvailabilityStatus.Available;
        var differentStatus = ProductAvailabilityStatus.OutOfStock;

        sameStatus1.Should().Be(sameStatus2);
        sameStatus1.Should().NotBe(differentStatus);
        (sameStatus1 == sameStatus2).Should().BeTrue();
        (sameStatus1 == differentStatus).Should().BeFalse();
    }

    [Fact]
    public void ProductAvailabilityStatus_FromCode_ShouldBeEqualToStaticInstance()
    {
        var status1 = ProductAvailabilityStatus.Available;
        var statusResult2 = ProductAvailabilityStatus.FromCode(1);

        statusResult2.Should().BeSuccess();
        var status2 = statusResult2.Value!;
        status1.Should().Be(status2);
        (status1 == status2).Should().BeTrue();
    }
}
