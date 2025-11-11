using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Testing.ValueObjectAssertions;

public static class HierarchicalPathAssertionExtensions
{
    public static HierarchicalPathAssertions Should(this HierarchicalPath? instance) => new(instance);
}

public sealed class HierarchicalPathAssertions : ReferenceTypeAssertions<HierarchicalPath?, HierarchicalPathAssertions>
{
    public HierarchicalPathAssertions(HierarchicalPath? instance) : base(instance) { }

    protected override string Identifier => "hierarchical path";

    public AndConstraint<HierarchicalPathAssertions> HaveValue(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected {context:hierarchical path} to not be null{reason}.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(string.Equals(Subject!.Value, expected, StringComparison.Ordinal))
            .FailWith("Expected {context:hierarchical path} to be {0}{reason}, but found {1}.",
                expected,
                Subject.Value);

        return new AndConstraint<HierarchicalPathAssertions>(this);
    }
}
