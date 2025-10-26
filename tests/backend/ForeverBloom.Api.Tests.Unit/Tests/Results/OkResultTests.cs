using FluentAssertions;
using ForeverBloom.Api.Results;
using ForeverBloom.Testing.Common.BaseTestClasses;
using Microsoft.AspNetCore.Http;
using ForeverBloom.Api.Tests.Helpers;

namespace ForeverBloom.Api.Tests.Results;

public sealed class OkResultTests : TestClassBase
{
    private sealed record SamplePayload(int Id, string Name);

    [Fact]
    public async Task ExecuteAsync_ShouldWriteJsonWithStatus200()
    {
        var expectedPayload = new SamplePayload(1, "Rose");
        var result = ApiResults.Ok(expectedPayload);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        httpContext.Response.ContentType.Should().Be(ContentTypes.Json);

        var actualPayload = await HttpContextTestHelper.ReadResponseAsync<SamplePayload>(
            httpContext,
            TestContext.Current.CancellationToken);
        actualPayload.Should().BeEquivalentTo(expectedPayload);
    }

}
