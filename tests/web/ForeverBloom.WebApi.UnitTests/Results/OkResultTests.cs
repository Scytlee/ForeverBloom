using FluentAssertions;
using ForeverBloom.WebApi.Results;
using ForeverBloom.WebApi.UnitTests.TestInfrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class OkResultTests
{
    private sealed record SamplePayload(int Id, string Name);

    [Fact]
    public async Task ExecuteAsync_ShouldProduce200OkResponseWithJsonPayload()
    {
        var expectedPayload = new SamplePayload(1, "Rose");
        var result = ApiResults.Ok(expectedPayload);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        httpContext.Response.ContentType.Should().Be(ContentTypes.Json);

        var actualPayload = await HttpContextTestHelper.ReadResponseAsync<SamplePayload>(
            httpContext,
            CancellationToken.None);

        actualPayload.Should().BeEquivalentTo(expectedPayload);
    }

    [Fact]
    public void Value_ShouldContainProvidedPayload()
    {
        var expectedPayload = new SamplePayload(42, "Tulip");

        var result = ApiResults.Ok(expectedPayload);

        result.Value.Should().BeEquivalentTo(expectedPayload);
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
