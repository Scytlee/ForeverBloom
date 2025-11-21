using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Errors;

public abstract record ApplicationError : IError
{
    public abstract string Code { get; }
    public abstract string Message { get; }

    public override string ToString() => $"[{Code}] {Message}";
}
