using FluentAssertions;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Results;
using ForeverBloom.Api.Tests.Helpers;
using ForeverBloom.Testing.Common.BaseTestClasses;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.Api.Tests.Results;

public sealed class UnauthorizedResultTests : TestClassBase
{
    [Fact]
    public async Task ExecuteAsync_ShouldWriteProblemDetailsWithStatus401()
    {
        var result = ApiResults.Unauthorized();
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<ProblemDetails>(
            httpContext,
            TestContext.Current.CancellationToken);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status401Unauthorized);
        payload.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.2");
        payload.Title.Should().Be("Unauthorized");
    }
}
