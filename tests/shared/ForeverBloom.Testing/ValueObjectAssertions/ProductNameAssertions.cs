using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Testing.ValueObjectAssertions;

public static class ProductNameAssertionExtensions
{
    public static ProductNameAssertions Should(this ProductName? instance) => new(instance);
}

public sealed class ProductNameAssertions : ReferenceTypeAssertions<ProductName?, ProductNameAssertions>
{
    public ProductNameAssertions(ProductName? subject) : base(subject) { }

    protected override string Identifier => "product name";

    public AndConstraint<ProductNameAssertions> HaveValue(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected {context:product name} to not be null{reason}.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(string.Equals(Subject!.Value, expected, StringComparison.Ordinal))
            .FailWith("Expected {context:product name} to be {0}{reason}, but found {1}.",
                expected,
                Subject.Value);

        return new AndConstraint<ProductNameAssertions>(this);
    }
}
