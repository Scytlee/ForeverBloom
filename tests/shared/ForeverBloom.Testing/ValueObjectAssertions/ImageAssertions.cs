using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Testing.ValueObjectAssertions;

public static class ImageAssertionExtensions
{
    public static ImageAssertions Should(this Image? instance) => new(instance);
}

public sealed class ImageAssertions : ReferenceTypeAssertions<Image?, ImageAssertions>
{
    public ImageAssertions(Image? instance) : base(instance) { }

    protected override string Identifier => "image";

    public AndConstraint<ImageAssertions> Match(
        string expectedSource,
        string? expectedAltText,
        string because = "",
        params object[] becauseArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSource);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected {context:image} to not be null{reason}.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(string.Equals(Subject!.Source.Value, expectedSource, StringComparison.Ordinal))
            .FailWith("Expected {context:image} source to be {0}{reason}, but found {1}.",
                expectedSource,
                Subject.Source.Value);

        var altTextScope = Execute.Assertion
            .BecauseOf(because, becauseArgs);

        if (expectedAltText is null)
        {
            altTextScope
                .ForCondition(Subject.AltText is null)
                .FailWith("Expected {context:image} alt text to be null{reason}, but found {0}.",
                    Subject.AltText);
        }
        else
        {
            altTextScope
                .ForCondition(string.Equals(Subject.AltText, expectedAltText, StringComparison.Ordinal))
                .FailWith("Expected {context:image} alt text to be {0}{reason}, but found {1}.",
                    expectedAltText,
                    Subject.AltText);
        }

        return new AndConstraint<ImageAssertions>(this);
    }
}
