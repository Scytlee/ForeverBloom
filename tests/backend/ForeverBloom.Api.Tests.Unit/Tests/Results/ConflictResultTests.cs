using System.Text.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Results;
using ForeverBloom.Api.Tests.Helpers;
using ForeverBloom.Testing.Common.BaseTestClasses;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.Api.Tests.Results;

public sealed class ConflictResultTests : TestClassBase
{
    [Fact]
    public async Task ExecuteAsync_ShouldWriteProblemDetailsWithErrorCode()
    {
        const string errorCode = "conflict.already-exists";
        var result = ApiResults.Conflict(errorCode);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<ProblemDetails>(
            httpContext,
            TestContext.Current.CancellationToken);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status409Conflict);
        payload.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.10");
        payload.Title.Should().Be("Conflict");

        const string errorCodeKey = "errorCode";
        payload.Extensions.Should().ContainKey(errorCodeKey);
        var errorCodeElement = (JsonElement)payload.Extensions[errorCodeKey]!;
        errorCodeElement.GetString().Should().Be(errorCode);
    }
}
