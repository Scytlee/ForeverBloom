using FluentAssertions;
using FluentAssertions.Execution;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Testing.Result;

/// <summary>
/// Entry points for FluentAssertions-style Result assertions.
/// </summary>
public static class ResultAssertionExtensions
{
    /// <summary>
    /// Entry point for assertions on non-generic results (Result or any IResult).
    /// </summary>
    public static ResultAssertions Should(this IResult? instance) =>
        new(instance);

    /// <summary>
    /// Entry point for assertions on generic results (Result&lt;T&gt;).
    /// </summary>
    public static ResultAssertions<T> Should<T>(this Result<T>? instance) =>
        new(instance);
}

/// <summary>
/// Assertions for IResult (no value).
/// </summary>
public sealed class ResultAssertions
{
    public ResultAssertions(IResult? subject)
    {
        _subject = subject;
    }

    private readonly IResult? _subject;

    public AndConstraint<ResultAssertions> BeSuccess(
        string because = "",
        params object[] becauseArgs)
    {
        ResultAssertionsInternal.BeSuccessInternal(_subject, because, becauseArgs);

        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> BeFailure(
        string because = "",
        params object[] becauseArgs)
    {
        ResultAssertionsInternal.BeFailureInternal(_subject, because, becauseArgs);

        return new AndConstraint<ResultAssertions>(this);
    }

    public TError HaveError<TError>(
        string because = "",
        params object[] becauseArgs)
        where TError : class, IError
    {
        var matching = ResultAssertionsInternal.HaveErrorInternal<TError>(_subject, because, becauseArgs);

        return matching;
    }

    public TError HaveSingleError<TError>(
        string because = "",
        params object[] becauseArgs)
        where TError : class, IError
    {
        var matching = ResultAssertionsInternal.HaveSingleErrorInternal<TError>(_subject, because, becauseArgs);

        return matching;
    }

    public IReadOnlyList<TError> HaveMultipleErrors<TError>(
        string because = "",
        params object[] becauseArgs)
        where TError : class, IError
    {
        var matching = ResultAssertionsInternal.HaveMultipleErrorsInternal<TError>(_subject, because, becauseArgs);

        return matching;
    }
}

/// <summary>
/// Assertions for Result&lt;T&gt; (with value).
/// </summary>
public sealed class ResultAssertions<T>
{
    public ResultAssertions(Result<T>? subject)
    {
        _subject = subject;
    }

    private readonly Result<T>? _subject;

    public AndConstraint<ResultAssertions<T>> BeSuccess(
        string because = "",
        params object[] becauseArgs)
    {
        ResultAssertionsInternal.BeSuccessInternal(_subject, because, becauseArgs);

        return new AndConstraint<ResultAssertions<T>>(this);
    }

    public AndConstraint<ResultAssertions<T>> BeFailure(
        string because = "",
        params object[] becauseArgs)
    {
        ResultAssertionsInternal.BeFailureInternal(_subject, because, becauseArgs);

        return new AndConstraint<ResultAssertions<T>>(this);
    }

    public TError HaveError<TError>(
        string because = "",
        params object[] becauseArgs)
        where TError : class, IError
    {
        var matching = ResultAssertionsInternal.HaveErrorInternal<TError>(_subject, because, becauseArgs);

        return matching;
    }

    public TError HaveSingleError<TError>(
        string because = "",
        params object[] becauseArgs)
        where TError : class, IError
    {
        var matching = ResultAssertionsInternal.HaveSingleErrorInternal<TError>(_subject, because, becauseArgs);

        return matching;
    }

    public IReadOnlyList<TError> HaveMultipleErrors<TError>(
        string because = "",
        params object[] becauseArgs)
        where TError : class, IError
    {
        var matching = ResultAssertionsInternal.HaveMultipleErrorsInternal<TError>(_subject, because, becauseArgs);

        return matching;
    }

    /// <summary>
    /// Asserts that the result is a success with the specified value (using .Equals).
    /// </summary>
    public AndConstraint<ResultAssertions<T>> HaveValue(
        T expected,
        string because = "",
        params object[] becauseArgs)
    {
        ResultAssertionsInternal.BeSuccessInternal(_subject, because, becauseArgs);

        _subject!.Value.Should().Be(expected, because, becauseArgs);

        return new AndConstraint<ResultAssertions<T>>(this);
    }
}

internal static class ResultAssertionsInternal
{
    internal static void BeSuccessInternal(IResult? result, string because = "", params object[] becauseArgs)
    {
        result.ExecuteResultIsNotNullAssertion(because, becauseArgs);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(result!.IsSuccess)
            .FailWith("Expected result to be success{reason}, but it was failure with error {0}.", result.Error);
    }

    internal static void BeFailureInternal(IResult? result, string because = "", params object[] becauseArgs)
    {
        result.ExecuteResultIsNotNullAssertion(because, becauseArgs);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!result!.IsSuccess)
            .FailWith("Expected result to be failure{reason}, but it was success.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(result.Error is not null)
            .FailWith("Expected failure result to have an error{reason}, but found <null>.");
    }

    internal static TError HaveErrorInternal<TError>(IResult? result, string because = "", params object[] becauseArgs)
    {
        BeFailureInternal(result, because, becauseArgs);

        var error = result!.Error!;
        var allErrors = error is CompositeError composite
            ? composite.Errors
            : [error];

        var matching = allErrors.OfType<TError>().ToList();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(matching.Count > 0)
            .FailWith(
                "Expected failure result to contain an error of type {0}{reason}, but found {1}.",
                typeof(TError).Name,
                allErrors);

        return matching[0];
    }

    internal static TError HaveSingleErrorInternal<TError>(IResult? result, string because = "", params object[] becauseArgs)
    {
        BeFailureInternal(result, because, becauseArgs);

        var error = result!.Error!;
        var allErrors = error is CompositeError composite
            ? composite.Errors
            : [error];

        var matching = allErrors.OfType<TError>().ToList();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(matching.Count == 1)
            .FailWith(
                "Expected failure result to contain a single error of type {0}{reason}, but found {1}.",
                typeof(TError).Name,
                matching.Count);

        return matching[0];
    }

    internal static IReadOnlyList<TError> HaveMultipleErrorsInternal<TError>(IResult? result, string because = "", params object[] becauseArgs)
    {
        BeFailureInternal(result, because, becauseArgs);

        var error = result!.Error!;
        var allErrors = error is CompositeError composite
            ? composite.Errors
            : [error];

        var matching = allErrors.OfType<TError>().ToList();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(matching.Count > 1)
            .FailWith(
                "Expected failure result to contain more than one error of type {0}{reason}, but found {1}.",
                typeof(TError).Name,
                matching.Count);

        return matching;
    }

    private static void ExecuteResultIsNotNullAssertion(this IResult? result, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(result is not null)
            .FailWith("Expected a non-null result{reason}, but found <null>., but found <null>.");
    }
}
