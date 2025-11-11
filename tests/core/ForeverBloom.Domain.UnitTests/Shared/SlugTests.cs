using FluentAssertions;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Shared;

public sealed class SlugTests
{
    [Theory]
    [InlineData("foreverbloom")]
    [InlineData("forever-bloom")]
    [InlineData("forever-bloom-123")]
    [InlineData("a")]
    public void Create_ShouldSucceed_ForValidInput(string value)
    {
        var slugResult = Slug.Create(value);

        slugResult.Should().BeSuccess();
        var slug = slugResult.Value!;
        slug.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ShouldFail_ForNullOrWhitespaceInput(string? value)
    {
        var slugResult = Slug.Create(value!);

        slugResult.Should().BeFailure();
        slugResult.Should().HaveError<SlugErrors.Empty>();
    }

    [Fact]
    public void Create_ShouldFail_ForTooLongInput()
    {
        var slugResult = Slug.Create(new string('a', Slug.MaxLength + 1));

        slugResult.Should().BeFailure();
        slugResult.Should().HaveError<SlugErrors.TooLong>();
    }

    [Theory]
    [InlineData("Uppercase")]
    [InlineData("with space")]
    [InlineData("with_underscore")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--dash")]
    public void Create_ShouldFail_ForInputWithInvalidFormat(string value)
    {
        var slugResult = Slug.Create(value);

        slugResult.Should().BeFailure();
        slugResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }

    [Fact]
    public void Create_ShouldFailWithAllRelevantErrors()
    {
        // both TooLong and InvalidFormat
        var slugResult = Slug.Create(new string('A', Slug.MaxLength + 1));

        slugResult.Should().BeFailure();
        slugResult.Should().HaveError<SlugErrors.TooLong>();
        slugResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }

    [Fact]
    public void Slug_ShouldHaveStructuralEquality()
    {
        var slug1 = SlugFactory.Create("forever-bloom");
        var slug2 = SlugFactory.Create("forever-bloom");

        slug1.Should().Be(slug2);
        (slug1 == slug2).Should().BeTrue();
    }
}
