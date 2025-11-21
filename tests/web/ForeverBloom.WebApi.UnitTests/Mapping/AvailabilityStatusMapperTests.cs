using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.UnitTests.Mapping;

public sealed class AvailabilityStatusMapperTests
{
    [Theory]
    [InlineData(1, "available")]
    [InlineData(2, "out_of_stock")]
    [InlineData(3, "made_to_order")]
    [InlineData(5, "discontinued")]
    [InlineData(6, "coming_soon")]
    public void ToString_ShouldReturnCorrectString_WhenGivenValidCode(int code, string expectedName)
    {
        var result = AvailabilityStatusMapper.ToString(code);

        result.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(999)]
    public void ToString_ShouldThrowArgumentOutOfRangeException_WhenGivenInvalidCode(int invalidCode)
    {
        var act = () => AvailabilityStatusMapper.ToString(invalidCode);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Invalid availability status code*")
            .And.ParamName.Should().Be("code");
    }

    [Fact]
    public void ToString_ShouldReturnStatusName_WhenGivenProductAvailabilityStatus()
    {
        foreach (var status in ProductAvailabilityStatus.All)
        {
            var result = AvailabilityStatusMapper.ToString(status);

            result.Should().Be(status.Name);
        }
    }

    [Fact]
    public void ToString_ShouldReturnSameResult_WhenGivenStatusOrItsCode()
    {
        foreach (var status in ProductAvailabilityStatus.All)
        {
            var resultFromStatus = AvailabilityStatusMapper.ToString(status);
            var resultFromCode = AvailabilityStatusMapper.ToString(status.Code);

            resultFromCode.Should().Be(resultFromStatus);
        }
    }
}
