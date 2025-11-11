using System.Diagnostics;
using FluentAssertions;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.SharedKernel.UnitTests.Optional;

public sealed class OptionalTests
{
    [Fact]
    public void Unset_ShouldCorrectlyCreateUnsetOptional()
    {
        var optional = Optional<int>.Unset;
        var valueAccessor = () => optional.Value;

        optional.IsSet.Should().BeFalse();
        optional.IsUnset.Should().BeTrue();
        valueAccessor.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FromValue_ShouldCorrectlyCreateSetOptionalWithValue()
    {
        const string value = "abc";
        var optional = Optional<string>.FromValue(value);

        optional.IsSet.Should().BeTrue();
        optional.IsUnset.Should().BeFalse();
        optional.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(false, null, "[Unset]")]
    [InlineData(true, null, "null")]
    [InlineData(true, 5, "5")]
    [InlineData(true, "test", "test")]
    public void ToString_ShouldReturnExpectedFormat(bool isSet, object? value, string expected)
    {
        var result = (isSet, value) switch
        {
            (false, _) => Optional<int>.Unset.ToString(),
            (true, null) => Optional<string?>.FromValue(null).ToString(),
            (true, int i) => Optional<int>.FromValue(i).ToString(),
            (true, string s) => Optional<string>.FromValue(s).ToString(),
            _ => throw new UnreachableException()
        };

        result.Should().Be(expected);
    }
}
