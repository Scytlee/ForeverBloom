using FluentValidation.Results;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Error representing validation failures from FluentValidation.
/// </summary>
public sealed record ValidationError(IReadOnlyList<ValidationFailure> Failures) : IError
{
    public string Code => "Validation.Failed";
    public string Message => "One or more validation errors occurred.";
}
