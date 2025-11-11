using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Client.Contracts;

public sealed record BadRequestError : IError
{
    public string Code => "BadRequest.Failed";
    public string Message { get; init; }

    public IReadOnlyList<ErrorDetail> Errors { get; init; }

    public BadRequestError(ErrorDetail[] errors)
    {
        Errors = errors;
        Message = errors.Length == 1
            ? errors[0].Message
            : "One or more errors occurred.";
    }
}
