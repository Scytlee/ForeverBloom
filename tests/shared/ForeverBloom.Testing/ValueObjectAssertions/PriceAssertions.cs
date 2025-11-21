using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Testing.ValueObjectAssertions;

public static class PriceAssertionExtensions
{
    public static PriceAssertions Should(this Money? instance) => new(instance);
}

public sealed class PriceAssertions : ReferenceTypeAssertions<Money?, PriceAssertions>
{
    public PriceAssertions(Money? subject) : base(subject) { }

    protected override string Identifier => "price";

    public AndConstraint<PriceAssertions> HaveValue(
        decimal expected,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected {context:price} to not be null{reason}.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject!.Value == expected)
            .FailWith("Expected {context:price} to equal {0}{reason}, but found {1}.",
                expected,
                Subject.Value);

        return new AndConstraint<PriceAssertions>(this);
    }
}
