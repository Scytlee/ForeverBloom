using FluentAssertions;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Results;
using ForeverBloom.Api.Tests.Helpers;
using ForeverBloom.Testing.Common.BaseTestClasses;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.Api.Tests.Results;

public sealed class ForbiddenResultTests : TestClassBase
{
    [Fact]
    public async Task ExecuteAsync_ShouldWriteProblemDetailsWithStatus403()
    {
        var result = ApiResults.Forbidden();
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<ProblemDetails>(
            httpContext,
            TestContext.Current.CancellationToken);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status403Forbidden);
        payload.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.4");
        payload.Title.Should().Be("Forbidden");
    }
}
