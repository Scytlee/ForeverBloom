using FluentAssertions;
using FluentAssertions.Execution;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.Testing.Optional;

/// <summary>
/// Entry point for FluentAssertions-style Optional assertions.
/// </summary>
public static class OptionalAssertionExtensions
{
    /// <summary>
    /// Entry point for assertions on Optional&lt;T&gt;.
    /// </summary>
    public static OptionalAssertions<T> Should<T>(this Optional<T> instance) =>
        new(instance);
}

/// <summary>
/// Assertions for Optional&lt;T&gt;.
/// </summary>
public sealed class OptionalAssertions<T>
{
    public OptionalAssertions(Optional<T> subject)
    {
        _subject = subject;
    }

    private readonly Optional<T> _subject;

    /// <summary>
    /// Asserts that the optional is unset.
    /// </summary>
    public AndConstraint<OptionalAssertions<T>> BeUnset(
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(_subject.IsUnset)
            .FailWith("Expected {context:optional} to be unset{reason}, but it was set to {0}.",
                _subject.IsSet ? _subject.Value : default);

        return new AndConstraint<OptionalAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the optional is set (without checking the value).
    /// </summary>
    public AndConstraint<OptionalAssertions<T>> BeSet(
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(_subject.IsSet)
            .FailWith("Expected {context:optional} to be set{reason}, but it was unset.");

        return new AndConstraint<OptionalAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the optional is set and has the expected value (using .Equals).
    /// </summary>
    public AndConstraint<OptionalAssertions<T>> BeSetTo(
        T expected,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(_subject.IsSet)
            .FailWith("Expected {context:optional} to be set to {0}{reason}, but it was unset.", expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(EqualityComparer<T>.Default.Equals(_subject.Value, expected))
            .FailWith("Expected {context:optional} to be set to {0}{reason}, but found {1}.", expected, _subject.Value);

        return new AndConstraint<OptionalAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the optional is set and the value is null (for reference types only).
    /// </summary>
    public AndConstraint<OptionalAssertions<T>> BeSetToNull(
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(_subject.IsSet)
            .FailWith("Expected {context:optional} to be set to null{reason}, but it was unset.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(_subject.Value is null)
            .FailWith("Expected {context:optional} to be set to null{reason}, but found {0}.", _subject.Value);

        return new AndConstraint<OptionalAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the optional is set and the value matches the predicate.
    /// </summary>
    public AndConstraint<OptionalAssertions<T>> HaveValueMatching(
        Func<T, bool> predicate,
        string because = "",
        params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(_subject.IsSet)
            .FailWith("Expected {context:optional} to be set with a value matching the predicate{reason}, but it was unset.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(predicate(_subject.Value))
            .FailWith("Expected {context:optional} value {0} to match the predicate{reason}, but it did not.", _subject.Value);

        return new AndConstraint<OptionalAssertions<T>>(this);
    }
}
