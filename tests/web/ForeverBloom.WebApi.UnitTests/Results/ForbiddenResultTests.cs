using FluentAssertions;
using ForeverBloom.WebApi.Results;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.UnitTests.TestInfrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class ForbiddenResultTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldProduce403ForbiddenResponse()
    {
        var result = ApiResults.Forbidden();
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<ProblemDetails>(
            httpContext,
            CancellationToken.None);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status403Forbidden);
        payload.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.4");
        payload.Title.Should().Be("Forbidden");
    }

    [Fact]
    public void Value_ShouldContainProblemDetails()
    {
        var result = ApiResults.Forbidden();

        result.Value.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }
}
