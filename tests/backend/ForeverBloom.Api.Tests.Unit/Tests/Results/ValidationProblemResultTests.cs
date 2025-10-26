using FluentAssertions;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Results;
using ForeverBloom.Api.Tests.Helpers;
using ForeverBloom.Testing.Common.BaseTestClasses;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.Api.Tests.Results;

public sealed class ValidationProblemResultTests : TestClassBase
{
    [Fact]
    public async Task ExecuteAsync_ShouldWriteValidationProblemDetailsWithStatus400()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "name", ["required"] },
            { "price", ["invalid"] }
        };
        var result = ApiResults.ValidationProblem(errors);
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        httpContext.Response.ContentType.Should().Be(ContentTypes.ProblemJson);

        var payload = await HttpContextTestHelper.ReadResponseAsync<ValidationProblemDetails>(
            httpContext,
            TestContext.Current.CancellationToken);

        payload.Should().NotBeNull();
        payload.Status.Should().Be(StatusCodes.Status400BadRequest);
        payload.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.1");
        payload.Title.Should().Be("Bad Request");
        payload.Errors.Should().ContainKey("name");
        payload.Errors.Should().ContainKey("price");
        payload.Errors["name"].Should().ContainSingle("required");
        payload.Errors["price"].Should().ContainSingle("invalid");
    }
}
