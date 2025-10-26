using System.Net;

namespace ForeverBloom.ApiClient.Contracts;

public record ApiResponse<T>
{
    public T? Value { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public bool IsSuccess { get; init; }
    public Error? Error { get; init; }
    public string? Location { get; init; }

    private ApiResponse(T? value, HttpStatusCode statusCode, bool isSuccess, Error? error = null, string? location = null)
    {
        Value = value;
        StatusCode = statusCode;
        IsSuccess = isSuccess;
        Error = error;
        Location = location;
    }

    public static ApiResponse<T> Success(T value, HttpStatusCode statusCode) => new(value, statusCode, true);
    public static ApiResponse<T> Failure(Error error, HttpStatusCode statusCode, string? location = null) => new(default, statusCode, false, error: error, location: location);
}
