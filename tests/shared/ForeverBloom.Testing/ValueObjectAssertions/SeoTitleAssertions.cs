using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using ForeverBloom.Domain.Shared;

namespace ForeverBloom.Testing.ValueObjectAssertions;

public static class SeoTitleAssertionExtensions
{
    public static SeoTitleAssertions Should(this SeoTitle? instance) => new(instance);
}

public sealed class SeoTitleAssertions : ReferenceTypeAssertions<SeoTitle?, SeoTitleAssertions>
{
    public SeoTitleAssertions(SeoTitle? instance) : base(instance) { }

    protected override string Identifier => "SEO title";

    public AndConstraint<SeoTitleAssertions> HaveValue(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected {context:SEO title} to not be null{reason}.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(string.Equals(Subject!.Value, expected, StringComparison.Ordinal))
            .FailWith("Expected {context:SEO title} to be {0}{reason}, but found {1}.",
                expected,
                Subject.Value);

        return new AndConstraint<SeoTitleAssertions>(this);
    }
}
