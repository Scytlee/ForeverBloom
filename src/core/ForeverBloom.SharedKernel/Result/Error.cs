namespace ForeverBloom.SharedKernel.Result;

public sealed record Error : IError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
}
