using FluentAssertions;
using ForeverBloom.Api.Results;
using ForeverBloom.Testing.Common.BaseTestClasses;
using Microsoft.AspNetCore.Http;
using ForeverBloom.Api.Tests.Helpers;

namespace ForeverBloom.Api.Tests.Results;

public sealed class PermanentRedirectResultTests : TestClassBase
{
    [Fact]
    public async Task ExecuteAsync_ShouldSetStatusAndLocationHeader()
    {
        const string redirectUrl = "https://example.com/permanent";
        var result = ApiResults.PermanentRedirect(redirectUrl);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status301MovedPermanently);
        httpContext.Response.Headers.Location.Should().ContainSingle(redirectUrl);
    }
}
