using System.Diagnostics.CodeAnalysis;
using System.Net;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Client.Contracts;

public sealed class HttpResult<T> : IResult
{
    public T? Value { get; }
    public IError? Error { get; }
    public HttpStatusCode StatusCode { get; }
    public string? Location { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => !IsSuccess;

    private HttpResult(bool isSuccess, T? value, IError? error, HttpStatusCode statusCode, string? location = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
        Location = location;
    }

    public static HttpResult<T> Success(T value, HttpStatusCode statusCode)
        => new(true, value, null, statusCode);

    public static HttpResult<T> Failure(IError error, HttpStatusCode statusCode, string? location = null)
        => new(false, default, error, statusCode, location);
}
