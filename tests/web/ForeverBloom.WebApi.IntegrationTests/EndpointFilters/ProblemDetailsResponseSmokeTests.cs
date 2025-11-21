using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.WebApi.Models;

namespace ForeverBloom.WebApi.IntegrationTests.EndpointFilters;

public sealed class ProblemDetailsResponseSmokeTests : WebApiIntegrationTestBase
{
    private static string BaseEndpointUrl => "/api/v1/admin/categories";

    [Fact]
    public async Task Endpoint_ShouldHaveProblemDetailsEnrichedByEndpointFilter_On400BadRequest()
    {
        var client = CreateClient(WebApiKeyScope.Admin);

        var httpResponse = await client.GetAsync($"{BaseEndpointUrl}?pageNumber=0", CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        httpResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await httpResponse.Content.ReadFromJsonAsync<BadRequestProblemDetails>(CancellationToken);
        problemDetails.Should().NotBeNull();
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.1");
        problemDetails.Title.Should().Be("Bad Request");
        problemDetails.Status.Should().Be(400);
        problemDetails.Instance.Should().Be("GET /api/v1/admin/categories");

        problemDetails.Errors.Should().OnlyContain(e =>
            e.Code == "Pagination.InvalidPageNumber"
            && !string.IsNullOrEmpty(e.Message));

        problemDetails.Extensions.Should().ContainKey("requestId").WhoseValue.Should().NotBeNull();
        problemDetails.Extensions.Should().ContainKey("traceId").WhoseValue.Should().NotBeNull();
    }

    [Fact]
    public async Task Endpoint_ShouldHaveProblemDetailsEnrichedByApiKeyAuthenticationHandler_On401Unauthorized()
    {
        var client = CreateClient();

        var httpResponse = await client.GetAsync(BaseEndpointUrl, CancellationToken);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        httpResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        problemDetails.Should().NotBeNull();
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.2");
        problemDetails.Title.Should().Be("Unauthorized");
        problemDetails.Status.Should().Be(401);
        problemDetails.Instance.Should().Be("GET /api/v1/admin/categories");

        problemDetails.Extensions.Should().ContainKey("requestId").WhoseValue.Should().NotBeNull();
        problemDetails.Extensions.Should().ContainKey("traceId").WhoseValue.Should().NotBeNull();
    }
}
