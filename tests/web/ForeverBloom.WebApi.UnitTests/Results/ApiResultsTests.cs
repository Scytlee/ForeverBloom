using FluentAssertions;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.Results;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class ApiResultsTests
{
    [Fact]
    public void Ok_ShouldReturnOkResultWithValue()
    {
        var value = new { Id = 1, Name = "Sample" };

        var result = ApiResults.Ok(value);

        result.Value.Should().BeEquivalentTo(value);
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void Created_ShouldReturnCreatedResultWithLocationAndValue()
    {
        var value = new { Id = 42, Name = "Created Item" };
        const string location = "/api/v1/items/42";

        var result = ApiResults.Created(location, value);

        result.Value.Should().BeEquivalentTo(value);
        result.Location.Should().Be(location);
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public void NoContent_ShouldReturnNoContentResult()
    {
        var result = ApiResults.NoContent();

        result.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void PermanentRedirect_ShouldReturnRedirectResultWithStatusCode301()
    {
        var result = ApiResults.PermanentRedirect("https://example.com/path");

        result.StatusCode.Should().Be(StatusCodes.Status301MovedPermanently);
    }

    [Fact]
    public void BadRequest_WithSingleError_ShouldReturnBadRequestResult()
    {
        var error = new TestError("Validation.Failed", "Validation failed");

        var result = ApiResults.BadRequest(error);

        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Value.Should().NotBeNull();
        result.Value.Errors.Should().BeEquivalentTo([
            new ErrorDetail { Code = error.Code, Message = error.Message }
        ]);
    }

    [Fact]
    public void BadRequest_WithCompositeError_ShouldUnwrapAllErrors()
    {
        var error1 = new TestError("Error.One", "First error");
        var error2 = new TestError("Error.Two", "Second error");
        var compositeError = new CompositeError([error1, error2]);

        var result = ApiResults.BadRequest(compositeError);

        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Value.Errors.Should().BeEquivalentTo([
            new ErrorDetail { Code = error1.Code, Message = error1.Message },
            new ErrorDetail { Code = error2.Code, Message = error2.Message }
        ]);
    }

    [Fact]
    public void Unauthorized_ShouldReturnProblemDetailsResult()
    {
        var result = ApiResults.Unauthorized();

        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void Forbidden_ShouldReturnProblemDetailsResult()
    {
        var result = ApiResults.Forbidden();

        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void NotFound_ShouldReturnProblemDetailsResult()
    {
        var result = ApiResults.NotFound();

        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Conflict_ShouldIncludeErrorCodeInProblemDetails()
    {
        const string errorCode = "conflict.code";

        var result = ApiResults.Conflict(errorCode);

        result.Value.Extensions.Should().ContainKey("errorCode");
        result.Value.Extensions["errorCode"].Should().Be(errorCode);
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    private sealed record TestError(string Code, string Message) : IError;
}
