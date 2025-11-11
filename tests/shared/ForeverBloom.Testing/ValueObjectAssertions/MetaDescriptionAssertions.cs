using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using ForeverBloom.Domain.Shared;

namespace ForeverBloom.Testing.ValueObjectAssertions;

public static class MetaDescriptionAssertionExtensions
{
    public static MetaDescriptionAssertions Should(this MetaDescription? instance) => new(instance);
}

public sealed class MetaDescriptionAssertions : ReferenceTypeAssertions<MetaDescription?, MetaDescriptionAssertions>
{
    public MetaDescriptionAssertions(MetaDescription? subject) : base(subject) { }

    protected override string Identifier => "meta description";

    public AndConstraint<MetaDescriptionAssertions> HaveValue(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected {context:meta description} to not be null{reason}.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(string.Equals(Subject!.Value, expected, StringComparison.Ordinal))
            .FailWith("Expected {context:meta description} to be {0}{reason}, but found {1}.",
                expected,
                Subject.Value);

        return new AndConstraint<MetaDescriptionAssertions>(this);
    }
}
