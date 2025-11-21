using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Client.Contracts;

/// <summary>
/// Represents an error encountered by the API client.
/// </summary>
public sealed record ApiClientError(string Code, string Message) : IError
{
    public override string ToString() => $"[{Code}] {Message}";
}
