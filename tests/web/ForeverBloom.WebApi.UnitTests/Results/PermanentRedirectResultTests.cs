using FluentAssertions;
using ForeverBloom.WebApi.Results;
using ForeverBloom.WebApi.UnitTests.TestInfrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class PermanentRedirectResultTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldProduce301MovedPermanentlyResponseWithLocationHeader()
    {
        const string redirectUrl = "https://example.com/permanent";
        var result = ApiResults.PermanentRedirect(redirectUrl);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status301MovedPermanently);
        httpContext.Response.Headers.Location.Should().ContainSingle(redirectUrl);
    }
}
