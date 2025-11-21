using FluentAssertions;
using ForeverBloom.WebApi.Results;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.UnitTests.TestInfrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class UnauthorizedResultTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldProduce401UnauthorizedResponse()
    {
        var result = ApiResults.Unauthorized();
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<ProblemDetails>(
            httpContext,
            CancellationToken.None);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status401Unauthorized);
        payload.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.2");
        payload.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public void Value_ShouldContainProblemDetails()
    {
        var result = ApiResults.Unauthorized();

        result.Value.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
