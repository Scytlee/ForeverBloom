using System.Diagnostics.CodeAnalysis;

namespace ForeverBloom.Api.Results;

public static class ApiResults
{
    public static OkResult<T> Ok<T>(T value) => new(value);

    public static CreatedResult<T> Created<T>([StringSyntax(StringSyntaxAttribute.Uri)] string location, T value) => new(location, value);

    public static NoContentResult NoContent() => new();

    public static PermanentRedirectResult PermanentRedirect([StringSyntax(StringSyntaxAttribute.Uri)] string location) => new(location);

    public static ValidationProblemResult ValidationProblem(IDictionary<string, string[]> errors) => new(errors);

    public static ValidationProblemResult ValidationProblem(string propertyName, string errorCode) => new(propertyName, errorCode);

    public static UnauthorizedResult Unauthorized() => new();

    public static ForbiddenResult Forbidden() => new();

    public static NotFoundResult NotFound() => new();

    public static ConflictResult Conflict(string errorCode) => new(errorCode);
}
