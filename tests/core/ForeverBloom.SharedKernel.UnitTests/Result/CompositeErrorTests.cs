using FluentAssertions;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.SharedKernel.UnitTests.Result;

public sealed class CompositeErrorTests
{
    private readonly Error _testError1 = new() { Code = "TEST.1", Message = "Test error 1" };
    private readonly Error _testError2 = new() { Code = "TEST.2", Message = "Test error 2" };
    private readonly Error _testError3 = new() { Code = "TEST.3", Message = "Test error 3" };

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
