using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Abstractions.Errors;

public abstract record DomainError : IError
{
    public abstract string Code { get; }
    public abstract string Message { get; }

    public override string ToString() => $"[{Code}] {Message}";
}
