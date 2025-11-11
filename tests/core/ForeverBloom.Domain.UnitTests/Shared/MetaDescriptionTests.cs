using FluentAssertions;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Shared;

public sealed class MetaDescriptionTests
{
    [Theory]
    [InlineData("Beautiful handcrafted roses for any occasion")]
    [InlineData("A")]
    [InlineData("Description with numbers 123 and symbols!?")]
    public void Create_ShouldSucceed_ForValidInput(string value)
    {
        var result = MetaDescription.Create(value);

        result.Should().BeSuccess();
        var metaDescription = result.Value!;
        metaDescription.Value.Should().Be(value);
    }

    [Fact]
    public void Create_ShouldSucceed_ForExactlyMaxLength()
    {
        var value = new string('a', MetaDescription.MaxLength);
        var result = MetaDescription.Create(value);

        result.Should().BeSuccess();
        var metaDescription = result.Value!;
        metaDescription.Value.Should().Be(value);
    }

    [Fact]
    public void Create_ShouldSucceed_AndTrimInput()
    {
        var result = MetaDescription.Create("  Trimmed Description  ");

        result.Should().BeSuccess();
        var metaDescription = result.Value!;
        metaDescription.Value.Should().Be("Trimmed Description");
    }

    [Fact]
    public void Create_ShouldSucceed_WithMultilineDescription()
    {
        var value = "First line\nSecond line";
        var result = MetaDescription.Create(value);

        result.Should().BeSuccess();
        var metaDescription = result.Value!;
        metaDescription.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void Create_ShouldFail_ForNullOrWhitespaceInput(string? value)
    {
        var result = MetaDescription.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<MetaDescriptionErrors.Empty>();
    }

    [Fact]
    public void Create_ShouldFail_ForTooLongInput()
    {
        var value = new string('a', MetaDescription.MaxLength + 1);
        var result = MetaDescription.Create(value);

        result.Should().BeFailure();
        result.Should().HaveError<MetaDescriptionErrors.TooLong>();
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var result = MetaDescription.Create("Test Description");
        var metaDescription = result.Value!;

        metaDescription.ToString().Should().Be("Test Description");
    }

    [Fact]
    public void MetaDescription_ShouldHaveStructuralEquality()
    {
        var sameMetaDescription1 = MetaDescriptionFactory.Create("Same description");
        var sameMetaDescription2 = MetaDescriptionFactory.Create("Same description");
        var differentMetaDescription = MetaDescriptionFactory.Create("Different description");

        sameMetaDescription1.Should().Be(sameMetaDescription2);
        sameMetaDescription1.Should().NotBe(differentMetaDescription);
        (sameMetaDescription1 == sameMetaDescription2).Should().BeTrue();
        (sameMetaDescription1 == differentMetaDescription).Should().BeFalse();
    }
}
