using System.Text.Json;
using FluentAssertions;
using ForeverBloom.WebApi.Results;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.UnitTests.TestInfrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class ConflictResultTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldProduce409ConflictResponseWithErrorCode()
    {
        const string errorCode = "Application.ConcurrencyConflict";
        var result = ApiResults.Conflict(errorCode);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<ProblemDetails>(
            httpContext,
            CancellationToken.None);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status409Conflict);
        payload.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.10");
        payload.Title.Should().Be("Conflict");
        payload.Extensions.Should().ContainKey("errorCode");

        var errorCodeElement = (JsonElement)payload.Extensions["errorCode"]!;
        errorCodeElement.GetString().Should().Be(errorCode);
    }

    [Fact]
    public void Value_ShouldContainErrorCodeInExtensions()
    {
        const string errorCode = "conflict.test";

        var result = ApiResults.Conflict(errorCode);

        result.Value.Should().NotBeNull();
        result.Value.Extensions.Should().ContainKey("errorCode");
        result.Value.Extensions["errorCode"].Should().Be(errorCode);
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }
}
