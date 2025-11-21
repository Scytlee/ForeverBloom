using FluentAssertions;
using ForeverBloom.WebApi.Results;
using ForeverBloom.WebApi.UnitTests.TestInfrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.WebApi.UnitTests.Results;

public sealed class CreatedResultTests
{
    private sealed record SamplePayload(int Id, string Name);

    [Fact]
    public async Task ExecuteAsync_ShouldProduce201CreatedResponseWithJsonPayloadAndLocationHeader()
    {
        const string location = "https://example.com/api/v1/products/123";
        var expectedPayload = new SamplePayload(123, "Rose");
        var result = ApiResults.Created(location, expectedPayload);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        httpContext.Response.ContentType.Should().Be(ContentTypes.Json);
        httpContext.Response.Headers.Location.Should().ContainSingle(location);

        var actualPayload = await HttpContextTestHelper.ReadResponseAsync<SamplePayload>(
            httpContext,
            CancellationToken.None);

        actualPayload.Should().BeEquivalentTo(expectedPayload);
    }

    [Fact]
    public void Value_ShouldContainProvidedPayloadAndLocation()
    {
        var payload = new SamplePayload(1, "Tulip");
        const string location = "/api/v1/products/1";

        var result = ApiResults.Created(location, payload);

        result.Value.Should().BeEquivalentTo(payload);
        result.Location.Should().Be(location);
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
    }
}
