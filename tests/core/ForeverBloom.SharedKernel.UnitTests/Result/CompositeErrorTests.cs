using FluentAssertions;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.SharedKernel.UnitTests.Result;

public sealed class CompositeErrorTests
{
    private readonly TestError _testError1 = new("TEST.1", "Test error 1");
    private readonly TestError _testError2 = new("TEST.2", "Test error 2");
    private readonly TestError _testError3 = new("TEST.3", "Test error 3");

    [Fact]
    public void Constructor_ShouldFlattenNestedComposites_WhenGivenNestedCompositeError()
    {
        var innerComposite = new CompositeError([_testError1, _testError2]);
        var outerComposite = new CompositeError([innerComposite]);

        outerComposite.Errors.Should().BeEquivalentTo([_testError1, _testError2]);
        outerComposite.Code.Should().Be("Error.Composite");
        outerComposite.Message.Should().Be("One or more errors occurred.");
    }

    [Fact]
    public void Constructor_ShouldFlattenMixedErrors_WhenGivenBothPlainAndNestedErrors()
    {
        var innerComposite = new CompositeError([_testError1, _testError2]);
        var outerComposite = new CompositeError([innerComposite, _testError3]);

        outerComposite.Errors.Should().BeEquivalentTo([_testError1, _testError2, _testError3]);
        outerComposite.Code.Should().Be("Error.Composite");
        outerComposite.Message.Should().Be("One or more errors occurred.");
    }
}
