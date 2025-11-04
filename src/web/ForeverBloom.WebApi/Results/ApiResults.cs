using System.Diagnostics.CodeAnalysis;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.WebApi.Models;

namespace ForeverBloom.WebApi.Results;

public static class ApiResults
{
    public static OkResult<T> Ok<T>(T value) => new(value);

    public static CreatedResult<T> Created<T>([StringSyntax(StringSyntaxAttribute.Uri)] string location, T value) => new(location, value);

    public static NoContentResult NoContent() => new();

    public static PermanentRedirectResult PermanentRedirect([StringSyntax(StringSyntaxAttribute.Uri)] string location) => new(location);

    public static ValidationProblemResult ValidationProblem(ValidationError validationError) => new(validationError);

    public static ValidationProblemResult ValidationProblem(string propertyName, params ValidationErrorDetail[] errorDetails) =>
        new(propertyName, errorDetails);

    public static BadRequestResult BadRequest(IError error) => new(error);

    public static UnauthorizedResult Unauthorized() => new();

    public static ForbiddenResult Forbidden() => new();

    public static NotFoundResult NotFound() => new();

    public static ConflictResult Conflict(string errorCode) => new(errorCode);
}
