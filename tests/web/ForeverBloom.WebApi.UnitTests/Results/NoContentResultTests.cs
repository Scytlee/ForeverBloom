using FluentAssertions;
using ForeverBloom.WebApi.Results;
using ForeverBloom.WebApi.UnitTests.TestInfrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class NoContentResultTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldProduce204NoContentResponse()
    {
        var result = ApiResults.NoContent();
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        httpContext.Response.Body.Length.Should().Be(0);
    }
}
