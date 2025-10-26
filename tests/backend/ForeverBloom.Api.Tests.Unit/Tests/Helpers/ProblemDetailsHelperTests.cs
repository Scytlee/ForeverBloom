using FluentAssertions;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Testing.Common.BaseTestClasses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;

namespace ForeverBloom.Api.Tests.Helpers;

public sealed class ProblemDetailsHelperTests : TestClassBase
{
    [Theory]
    [InlineData(400, "https://tools.ietf.org/html/rfc9110#section-15.5.1", "Bad Request")]
    [InlineData(401, "https://tools.ietf.org/html/rfc9110#section-15.5.2", "Unauthorized")]
    [InlineData(403, "https://tools.ietf.org/html/rfc9110#section-15.5.4", "Forbidden")]
    [InlineData(404, "https://tools.ietf.org/html/rfc9110#section-15.5.5", "Not Found")]
    [InlineData(405, "https://tools.ietf.org/html/rfc9110#section-15.5.6", "Method Not Allowed")]
    [InlineData(406, "https://tools.ietf.org/html/rfc9110#section-15.5.7", "Not Acceptable")]
    [InlineData(408, "https://tools.ietf.org/html/rfc9110#section-15.5.9", "Request Timeout")]
    [InlineData(409, "https://tools.ietf.org/html/rfc9110#section-15.5.10", "Conflict")]
    [InlineData(412, "https://tools.ietf.org/html/rfc9110#section-15.5.13", "Precondition Failed")]
    [InlineData(415, "https://tools.ietf.org/html/rfc9110#section-15.5.16", "Unsupported Media Type")]
    [InlineData(422, "https://tools.ietf.org/html/rfc4918#section-11.2", "Unprocessable Entity")]
    [InlineData(426, "https://tools.ietf.org/html/rfc9110#section-15.5.22", "Upgrade Required")]
    [InlineData(500, "https://tools.ietf.org/html/rfc9110#section-15.6.1", "Internal Server Error")]
    [InlineData(502, "https://tools.ietf.org/html/rfc9110#section-15.6.3", "Bad Gateway")]
    [InlineData(503, "https://tools.ietf.org/html/rfc9110#section-15.6.4", "Service Unavailable")]
    [InlineData(504, "https://tools.ietf.org/html/rfc9110#section-15.6.5", "Gateway Timeout")]
    public void CreateProblemDetails_ShouldSetStatusTypeAndTitleCorrectly(int statusCode, string expectedType, string expectedTitle)
    {
        var result = ProblemDetailsHelper.CreateProblemDetails(statusCode);

        result.Status.Should().Be(statusCode);
        result.Type.Should().Be(expectedType);
        result.Title.Should().Be(expectedTitle);
        result.Extensions.Should().NotContainKey("errorCode");
    }

    [Fact]
    public void CreateProblemDetails_ShouldIncludeErrorCodeInExtensions_WhenProvided()
    {
        const string errorCode = "PRODUCT_NOT_FOUND";
        const int statusCode = 404;

        var result = ProblemDetailsHelper.CreateProblemDetails(statusCode, errorCode);

        result.Status.Should().Be(statusCode);
        result.Extensions.Should().ContainKey("errorCode");
        result.Extensions["errorCode"].Should().Be(errorCode);
    }

    [Fact]
    public void CreateProblemDetails_ShouldIncludeEmptyErrorCode_WhenProvidedAsEmptyString()
    {
        const string errorCode = "";
        const int statusCode = 400;

        var result = ProblemDetailsHelper.CreateProblemDetails(statusCode, errorCode);

        result.Status.Should().Be(statusCode);
        result.Extensions.Should().ContainKey("errorCode");
        result.Extensions["errorCode"].Should().Be("");
    }

    [Fact]
    public void CreateProblemDetails_ShouldUseDefaultMapping_ForUnknownStatusCode()
    {
        const int unknownStatusCode = 999;

        var result = ProblemDetailsHelper.CreateProblemDetails(unknownStatusCode);

        result.Status.Should().Be(unknownStatusCode);
        result.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15");
        result.Title.Should().Be("Error");
    }

    [Fact]
    public void CreateProblemDetails_ShouldUseDefaultMapping_ForZeroStatusCode()
    {
        const int statusCode = 0;

        var result = ProblemDetailsHelper.CreateProblemDetails(statusCode);

        result.Status.Should().Be(statusCode);
        result.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15");
        result.Title.Should().Be("Error");
    }

    [Fact]
    public void CreateValidationProblemDetails_ShouldBuild400ValidationEnvelope()
    {
        const int statusCode = 400;
        var errors = new Dictionary<string, string[]>
        {
            ["Name"] = ["Name is required", "Name must not be empty"],
            ["Email"] = ["Email format is invalid"]
        };

        var result = ProblemDetailsHelper.CreateValidationProblemDetails(statusCode, errors);

        result.Status.Should().Be(statusCode);
        result.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.1");
        result.Title.Should().Be("Bad Request");
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void CreateValidationProblemDetails_ShouldBuild422ValidationEnvelope()
    {
        const int statusCode = 422;
        var errors = new Dictionary<string, string[]>
        {
            ["Quantity"] = ["Quantity must be positive"]
        };

        var result = ProblemDetailsHelper.CreateValidationProblemDetails(statusCode, errors);

        result.Status.Should().Be(statusCode);
        result.Type.Should().Be("https://tools.ietf.org/html/rfc4918#section-11.2");
        result.Title.Should().Be("Unprocessable Entity");
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void EnrichProblemDetails_ForeverBloomContract_ShouldSetInstanceRequestIdAndTraceId()
    {
        const string method = "POST";
        const string path = "/api/v1/products";
        const string traceIdentifier = "test-request-123";
        var problemDetails = new ProblemDetails();
        var (httpContext, expectedActivityId) = CreateHttpContextWithActivityFeature(method, path, traceIdentifier);

        ProblemDetailsHelper.EnrichProblemDetails(problemDetails, httpContext);

        problemDetails.Instance.Should().Be($"{method} {path}");
        problemDetails.Extensions.Should().ContainKey("requestId");
        problemDetails.Extensions["requestId"].Should().Be(traceIdentifier);
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions["traceId"].Should().Be(expectedActivityId);
    }

    [Fact]
    public void EnrichProblemDetails_MicrosoftAspNetCore_ShouldSetInstanceRequestIdAndTraceId()
    {
        const string method = "GET";
        const string path = "/api/v1/categories";
        const string traceIdentifier = "test-request-123";
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails();
        var (httpContext, expectedActivityId) = CreateHttpContextWithActivityFeature(method, path, traceIdentifier);

        ProblemDetailsHelper.EnrichProblemDetails(problemDetails, httpContext);

        problemDetails.Instance.Should().Be($"{method} {path}");
        problemDetails.Extensions.Should().ContainKey("requestId");
        problemDetails.Extensions["requestId"].Should().Be(traceIdentifier);
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions["traceId"].Should().Be(expectedActivityId);
    }

    [Fact]
    public void EnrichProblemDetails_ForeverBloomContract_ShouldThrowArgumentNullException_WhenProblemDetailsIsNull()
    {
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        var act = () => ProblemDetailsHelper.EnrichProblemDetails((ProblemDetails)null!, httpContext);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnrichProblemDetails_ForeverBloomContract_ShouldThrowArgumentNullException_WhenHttpContextIsNull()
    {
        var problemDetails = new ProblemDetails();

        var act = () => ProblemDetailsHelper.EnrichProblemDetails(problemDetails, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnrichProblemDetails_MicrosoftAspNetCore_ShouldThrowArgumentNullException_WhenProblemDetailsIsNull()
    {
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        var act = () => ProblemDetailsHelper.EnrichProblemDetails((Microsoft.AspNetCore.Mvc.ProblemDetails)null!, httpContext);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnrichProblemDetails_MicrosoftAspNetCore_ShouldThrowArgumentNullException_WhenHttpContextIsNull()
    {
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails();

        var act = () => ProblemDetailsHelper.EnrichProblemDetails(problemDetails, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnrichProblemDetails_ShouldHandleNullActivity()
    {
        const string method = "DELETE";
        const string path = "/api/v1/products/1";
        const string traceIdentifier = "test-request-999";
        var problemDetails = new ProblemDetails();
        var httpContext = CreateHttpContextWithNullActivityFeature(method, path, traceIdentifier);

        ProblemDetailsHelper.EnrichProblemDetails(problemDetails, httpContext);

        problemDetails.Instance.Should().Be($"{method} {path}");
        problemDetails.Extensions.Should().ContainKey("requestId");
        problemDetails.Extensions["requestId"].Should().Be(traceIdentifier);
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions["traceId"].Should().BeNull();
    }

    [Fact]
    public void EnrichProblemDetails_ShouldHandleMissingActivityFeature()
    {
        const string method = "PATCH";
        const string path = "/api/v1/products/2";
        const string traceIdentifier = "missing-feature-001";
        var problemDetails = new ProblemDetails();
        var httpContext = CreateHttpContextWithoutActivityFeature(method, path, traceIdentifier);

        ProblemDetailsHelper.EnrichProblemDetails(problemDetails, httpContext);

        problemDetails.Instance.Should().Be($"{method} {path}");
        problemDetails.Extensions.Should().ContainKey("requestId");
        problemDetails.Extensions["requestId"].Should().Be(traceIdentifier);
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions["traceId"].Should().BeNull();
    }

    [Fact]
    public void EnrichProblemDetails_ShouldNotOverwriteExistingExtensions()
    {
        const string method = "GET";
        const string path = "/health";
        const string traceIdentifier = "new-request-id";
        var problemDetails = new ProblemDetails();
        problemDetails.Extensions["requestId"] = "original-request";
        problemDetails.Extensions["traceId"] = "original-trace";

        var (httpContext, expectedActivityId) = CreateHttpContextWithActivityFeature(method, path, traceIdentifier);

        ProblemDetailsHelper.EnrichProblemDetails(problemDetails, httpContext);

        problemDetails.Extensions["requestId"].Should().Be("original-request");
        problemDetails.Extensions["traceId"].Should().Be("original-trace");
        problemDetails.Instance.Should().Be($"{method} {path}");
        expectedActivityId.Should().NotBeNull();
    }

    private static (DefaultHttpContext HttpContext, string? ActivityId) CreateHttpContextWithActivityFeature(string method, string path, string traceIdentifier)
    {
        var httpContext = HttpContextTestHelper.CreateHttpContext();

        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        httpContext.TraceIdentifier = traceIdentifier;

        var traceId = ActivityTraceId.CreateFromString("0123456789abcdef0123456789abcdef");
        var spanId = ActivitySpanId.CreateFromString("0123456789abcdef");

        var activity = new Activity("test-activity");
        activity.SetParentId(traceId, spanId, ActivityTraceFlags.Recorded);
        activity.Start();

        var httpActivityFeature = new TestHttpActivityFeature { Activity = activity };
        httpContext.Features.Set<IHttpActivityFeature>(httpActivityFeature);

        httpContext.Response.RegisterForDispose(activity);

        return (httpContext, activity.Id);
    }

    private static DefaultHttpContext CreateHttpContextWithNullActivityFeature(string method, string path, string traceIdentifier)
    {
        var httpContext = HttpContextTestHelper.CreateHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        httpContext.TraceIdentifier = traceIdentifier;

        var httpActivityFeature = new TestHttpActivityFeature { Activity = null! };
        httpContext.Features.Set<IHttpActivityFeature>(httpActivityFeature);
        return httpContext;
    }

    private static DefaultHttpContext CreateHttpContextWithoutActivityFeature(string method, string path, string traceIdentifier)
    {
        var httpContext = HttpContextTestHelper.CreateHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        httpContext.TraceIdentifier = traceIdentifier;
        // Intentionally do not set IHttpActivityFeature
        return httpContext;
    }

    private sealed class TestHttpActivityFeature : IHttpActivityFeature
    {
        public required Activity Activity { get; set; }
    }
}
