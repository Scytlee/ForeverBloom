using System.Net;
using FluentAssertions;
using ForeverBloom.Testing.Integration.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using ForeverBloom.WebApi.Authentication;

namespace ForeverBloom.WebApi.IntegrationTests.Authentication;

public sealed class ApiKeyAuthenticationHandlerSmokeTests : WebApiIntegrationTestBase
{
    private const string EndpointUrl = "/api/v1/admin/products";

    [Fact]
    public async Task Endpoint_ShouldRespondWith401UnauthorizedAndWwwAuthenticateHeader_WhenApiKeyIsMissing()
    {
        var client = Fixture.CreateClient();

        var response = await client.GetAsync(EndpointUrl, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().ContainSingle();
        var header = response.Headers.WwwAuthenticate.Single();
        header.Scheme.Should().Be(ApiKeyAuthenticationDefaults.SchemeName);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith403Forbidden_WhenApiKeyDoesNotMatchRequiredPolicy()
    {
        // CreateProduct requires AdminAccess policy
        var client = Fixture.CreateClient(WebApiKeyScope.Frontend);

        var response = await client.GetAsync(EndpointUrl, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
