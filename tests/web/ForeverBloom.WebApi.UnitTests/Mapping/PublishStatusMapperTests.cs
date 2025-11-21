using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.UnitTests.Mapping;

public sealed class PublishStatusMapperTests
{
    [Theory]
    [InlineData(1, "draft")]
    [InlineData(2, "published")]
    [InlineData(3, "hidden")]
    public void ToString_ShouldReturnCorrectString_WhenGivenValidCode(int code, string expectedName)
    {
        var result = PublishStatusMapper.ToString(code);

        result.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(999)]
    public void ToString_ShouldThrowArgumentOutOfRangeException_WhenGivenInvalidCode(int invalidCode)
    {
        var act = () => PublishStatusMapper.ToString(invalidCode);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Invalid publish status code*")
            .And.ParamName.Should().Be("code");
    }

    [Fact]
    public void ToString_ShouldReturnStatusName_WhenGivenPublishStatus()
    {
        foreach (var status in PublishStatus.All)
        {
            var result = PublishStatusMapper.ToString(status);

            result.Should().Be(status.Name);
        }
    }

    [Fact]
    public void ToString_ShouldReturnSameResult_WhenGivenStatusOrItsCode()
    {
        foreach (var status in PublishStatus.All)
        {
            var resultFromStatus = PublishStatusMapper.ToString(status);
            var resultFromCode = PublishStatusMapper.ToString(status.Code);

            resultFromCode.Should().Be(resultFromStatus);
        }
    }
}
