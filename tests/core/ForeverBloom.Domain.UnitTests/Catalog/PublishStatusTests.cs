using System.Diagnostics;
using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class PublishStatusTests
{
    [Theory]
    [InlineData(1, nameof(PublishStatus.Draft))]
    [InlineData(2, nameof(PublishStatus.Published))]
    [InlineData(3, nameof(PublishStatus.Hidden))]
    public void FromCode_ShouldReturnCorrectStatus_ForValidCodes(int code, string expectedStatusName)
    {
        var result = PublishStatus.FromCode(code);

        result.Should().BeSuccess();
        var status = result.Value!;
        status.Code.Should().Be(code);

        var expectedStatus = expectedStatusName switch
        {
            nameof(PublishStatus.Draft) => PublishStatus.Draft,
            nameof(PublishStatus.Published) => PublishStatus.Published,
            nameof(PublishStatus.Hidden) => PublishStatus.Hidden,
            _ => throw new UnreachableException()
        };

        status.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(999)]
    public void FromCode_ShouldFail_ForInvalidCodes(int code)
    {
        var result = PublishStatus.FromCode(code);

        result.Should().BeFailure();
        result.Should().HaveError<PublishStatusErrors.InvalidCode>();
    }

    [Theory]
    [InlineData(1, 2)] // Draft → Published
    [InlineData(1, 3)] // Draft → Hidden
    [InlineData(2, 3)] // Published → Hidden
    [InlineData(3, 2)] // Hidden → Published
    public void CanTransitionTo_ShouldReturnTrue_ForValidTransitions(int fromCode, int toCode)
    {
        var from = PublishStatus.FromCode(fromCode).Value!;
        var to = PublishStatus.FromCode(toCode).Value!;

        from.CanTransitionTo(to).Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 1)] // Draft → Draft (self)
    [InlineData(2, 2)] // Published → Published (self)
    [InlineData(2, 1)] // Published → Draft (backward)
    [InlineData(3, 3)] // Hidden → Hidden (self)
    [InlineData(3, 1)] // Hidden → Draft (backward)
    public void CanTransitionTo_ShouldReturnFalse_ForInvalidTransitions(int fromCode, int toCode)
    {
        var from = PublishStatus.FromCode(fromCode).Value!;
        var to = PublishStatus.FromCode(toCode).Value!;

        from.CanTransitionTo(to).Should().BeFalse();
    }
    [Fact]
    public void All_ShouldContainAllPredefinedStatuses()
    {
        PublishStatus.All.Should().HaveCount(3);
        PublishStatus.All.Should().Contain(PublishStatus.Draft);
        PublishStatus.All.Should().Contain(PublishStatus.Published);
        PublishStatus.All.Should().Contain(PublishStatus.Hidden);
    }

    [Fact]
    public void PublishStatus_ShouldHaveStructuralEquality()
    {
        var sameStatus1 = PublishStatus.Draft;
        var sameStatus2 = PublishStatus.Draft;
        var differentStatus = PublishStatus.Published;

        sameStatus1.Should().Be(sameStatus2);
        sameStatus1.Should().NotBe(differentStatus);
        (sameStatus1 == sameStatus2).Should().BeTrue();
        (sameStatus1 == differentStatus).Should().BeFalse();
    }

    [Fact]
    public void PublishStatus_FromCode_ShouldBeEqualToStaticInstance()
    {
        var status1 = PublishStatus.Draft;
        var statusResult2 = PublishStatus.FromCode(1);

        statusResult2.Should().BeSuccess();
        var status2 = statusResult2.Value!;
        status1.Should().Be(status2);
        (status1 == status2).Should().BeTrue();
    }
}
