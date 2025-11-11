using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using ForeverBloom.Domain.Shared;

namespace ForeverBloom.Testing.ValueObjectAssertions;

public static class SlugAssertionExtensions
{
    public static SlugAssertions Should(this Slug? instance) => new(instance);
}

public sealed class SlugAssertions : ReferenceTypeAssertions<Slug?, SlugAssertions>
{
    public SlugAssertions(Slug? instance) : base(instance) { }

    protected override string Identifier => "slug";

    public AndConstraint<SlugAssertions> HaveValue(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected {context:slug} to not be null{reason}.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(string.Equals(Subject!.Value, expected, StringComparison.Ordinal))
            .FailWith("Expected {context:slug} to be {0}{reason}, but found {1}.",
                expected,
                Subject.Value);

        return new AndConstraint<SlugAssertions>(this);
    }
}
