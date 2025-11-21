using System.Diagnostics.CodeAnalysis;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.WebApi.Results;

public static class ApiResults
{
    public static OkResult<T> Ok<T>(T value) => new(value);

    public static CreatedResult<T> Created<T>([StringSyntax(StringSyntaxAttribute.Uri)] string location, T value) => new(location, value);

    public static NoContentResult NoContent() => new();

    public static PermanentRedirectResult PermanentRedirect([StringSyntax(StringSyntaxAttribute.Uri)] string location) => new(location);

    public static BadRequestResult BadRequest(IError error) => new(error);

    public static UnauthorizedResult Unauthorized() => new();

    public static ForbiddenResult Forbidden() => new();

    public static NotFoundResult NotFound() => new();

    public static ConflictResult Conflict(string errorCode) => new(errorCode);
}
