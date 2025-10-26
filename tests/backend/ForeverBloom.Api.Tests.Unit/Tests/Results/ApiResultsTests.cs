using FluentAssertions;
using ForeverBloom.Api.Results;
using ForeverBloom.Testing.Common.BaseTestClasses;
using Microsoft.AspNetCore.Http;

namespace ForeverBloom.Api.Tests.Results;

public sealed class ApiResultsTests : TestClassBase
{
    [Fact]
    public void Ok_ShouldReturnOkResultWithValue()
    {
        var value = new { Id = 1, Name = "Sample" };

        var result = ApiResults.Ok(value);

        result.Value.Should().BeEquivalentTo(value);
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void PermanentRedirect_ShouldReturnRedirectResultWithStatusCode301()
    {
        var result = ApiResults.PermanentRedirect("https://example.com/path");

        result.StatusCode.Should().Be(StatusCodes.Status301MovedPermanently);
    }

    [Fact]
    public void ValidationProblem_WithDictionary_ShouldWrapErrors()
    {
        var errors = new Dictionary<string, string[]> { { "name", ["required"] } };

        var result = ApiResults.ValidationProblem(errors);

        result.Value.Errors.Should().ContainKey("name");
        result.Value.Errors["name"].Should().ContainSingle("required");
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ValidationProblem_WithProperty_ShouldCreateDictionary()
    {
        var result = ApiResults.ValidationProblem("name", "required");

        result.Value.Errors.Should().ContainKey("name");
        result.Value.Errors["name"].Should().ContainSingle("required");
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Unauthorized_ShouldReturnProblemDetailsResult()
    {
        var result = ApiResults.Unauthorized();

        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void Forbidden_ShouldReturnProblemDetailsResult()
    {
        var result = ApiResults.Forbidden();

        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void NotFound_ShouldReturnProblemDetailsResult()
    {
        var result = ApiResults.NotFound();

        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Conflict_ShouldIncludeErrorCodeInProblemDetails()
    {
        const string errorCode = "conflict.code";

        var result = ApiResults.Conflict(errorCode);

        result.Value.Extensions.Should().ContainKey("errorCode");
        result.Value.Extensions["errorCode"].Should().Be(errorCode);
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }
}
