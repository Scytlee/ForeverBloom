using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Testing.ValueObjectAssertions;

public static class HtmlFragmentAssertionExtensions
{
    public static HtmlFragmentAssertions Should(this HtmlFragment? instance) => new(instance);
}

public sealed class HtmlFragmentAssertions : ReferenceTypeAssertions<HtmlFragment?, HtmlFragmentAssertions>
{
    public HtmlFragmentAssertions(HtmlFragment? subject) : base(subject) { }

    protected override string Identifier => "HTML fragment";

    public AndConstraint<HtmlFragmentAssertions> HaveValue(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected {context:HTML fragment} to not be null{reason}.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(string.Equals(Subject!.Value, expected, StringComparison.Ordinal))
            .FailWith("Expected {context:HTML fragment} to equal {0}{reason}, but found {1}.",
                expected,
                Subject.Value);

        return new AndConstraint<HtmlFragmentAssertions>(this);
    }
}
