using FluentAssertions;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.WebApi.Results;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.UnitTests.TestInfrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class BadRequestResultTests
{
    private sealed record TestError(string Code, string Message) : IError;

    [Fact]
    public async Task ExecuteAsync_WithSingleError_ShouldProduce400BadRequestResponseWithSingleErrorDetail()
    {
        var error = new TestError("Validation.Failed", "Validation failed");
        var result = ApiResults.BadRequest(error);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<BadRequestProblemDetails>(
            httpContext,
            CancellationToken.None);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status400BadRequest);
        payload.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.1");
        payload.Title.Should().Be("Bad Request");
        payload.Errors.Should().BeEquivalentTo([
            new ErrorDetail { Code = error.Code, Message = error.Message }
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompositeError_ShouldProduce400BadRequestResponseWithMultipleErrorDetails()
    {
        var error1 = new TestError("Name.Required", "Name is required");
        var error2 = new TestError("Email.Invalid", "Email format is invalid");
        var compositeError = new CompositeError([error1, error2]);
        var result = ApiResults.BadRequest(compositeError);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<BadRequestProblemDetails>(
            httpContext,
            CancellationToken.None);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status400BadRequest);
        payload.Errors.Should().BeEquivalentTo([
            new ErrorDetail { Code = error1.Code, Message = error1.Message },
            new ErrorDetail { Code = error2.Code, Message = error2.Message }
        ]);
    }

    [Fact]
    public void Value_ShouldContainBadRequestProblemDetails()
    {
        var error = new TestError("Test.Error", "Test error message");

        var result = ApiResults.BadRequest(error);

        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<BadRequestProblemDetails>();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
