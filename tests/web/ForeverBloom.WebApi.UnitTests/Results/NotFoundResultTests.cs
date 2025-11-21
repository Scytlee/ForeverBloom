using FluentAssertions;
using ForeverBloom.WebApi.Results;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.UnitTests.TestInfrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class NotFoundResultTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldProduce404NotFoundResponse()
    {
        var result = ApiResults.NotFound();
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<ProblemDetails>(
            httpContext,
            CancellationToken.None);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status404NotFound);
        payload.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.5");
        payload.Title.Should().Be("Not Found");
    }

    [Fact]
    public void Value_ShouldContainProblemDetails()
    {
        var result = ApiResults.NotFound();

        result.Value.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
