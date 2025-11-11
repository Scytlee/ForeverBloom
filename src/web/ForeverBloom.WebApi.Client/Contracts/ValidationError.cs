using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Client.Contracts;

public sealed record ValidationError : IError
{
    public string Code => "Validation.Failed";
    public string Message => "One or more validation errors occurred.";

    public IReadOnlyDictionary<string, ValidationErrorDetail[]> Failures { get; init; }

    public ValidationError(IDictionary<string, ValidationErrorDetail[]> failures)
    {
        Failures = failures.AsReadOnly();
    }
}
