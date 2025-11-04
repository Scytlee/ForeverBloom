namespace ForeverBloom.SharedKernel.Result;

/// <summary>
/// Enables generic creation of failure results.
/// Implemented by Result and Result&lt;T&gt; to support type-safe failure creation in behaviors without reflection.
/// </summary>
/// <typeparam name="TSelf">The implementing result type (self-referencing).</typeparam>
// ReSharper disable once TypeParameterCanBeVariant
// TSelf is intentionally invariant
// Covariance adds no value here and would limit future extensibility
public interface ICreatesFailure<TSelf> where TSelf : ICreatesFailure<TSelf>
{
    /// <summary>
    /// Creates a failure result with the specified error.
    /// </summary>
    static abstract TSelf Failure(IError error);
}
