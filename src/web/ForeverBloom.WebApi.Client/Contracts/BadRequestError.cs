using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Client.Contracts;

public sealed record BadRequestError : IError
{
    public string Code => "BadRequest.Failed";
    public string Message => "One or more errors occurred.";

    public IReadOnlyList<ErrorDetail> Errors { get; init; }

    public BadRequestError(ErrorDetail[] errors)
    {
        Errors = errors;
    }
}
