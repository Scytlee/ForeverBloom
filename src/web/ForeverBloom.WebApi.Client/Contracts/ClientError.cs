using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Client.Contracts;

public sealed record ClientError : IError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required Exception Exception { get; init; }
}
