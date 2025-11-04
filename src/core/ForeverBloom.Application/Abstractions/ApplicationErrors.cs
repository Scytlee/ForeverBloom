using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions;

public static class ApplicationErrors
{
    /// <summary>
    /// Error indicating a concurrency conflict occurred while handling the use case.
    /// </summary>
    public sealed record ConcurrencyConflict : IError
    {
        public string Code => "Application.ConcurrencyConflict";
        public string Message => "Concurrency conflict occurred while handling the use case. Please reload and retry.";
    }
}
