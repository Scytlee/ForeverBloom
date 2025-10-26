using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Authentication;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Testing.Common.Fixtures.Database;

namespace ForeverBloom.Api.Tests.Authentication;

public sealed class ApiKeyAuthenticationHandlerSmokeTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/admin/products";

    public ApiKeyAuthenticationHandlerSmokeTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith401Unauthorized_AndWwwAuthenticateHeader_WhenApiKeyIsMissing()
    {
        BuildApp();
        var client = _app.RequestClient();
        var request = new CreateProductRequest();

        var response = await client.PostAsJsonAsync(EndpointUrl, request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().ContainSingle();
        var header = response.Headers.WwwAuthenticate.Single();
        header.Scheme.Should().Be(ApiKeyAuthenticationDefaults.SchemeName);
    }

    [Fact]
    public async Task Endpoint_ShouldRespondWith403Forbidden_WhenApiKeyDoesNotMatchRequiredPolicy()
    {
        BuildApp();
        var client = _app.RequestClient().UseFrontendKey();
        var request = new CreateProductRequest();

        // CreateProduct requires AdminAccess policy
        var response = await client.PostAsJsonAsync(EndpointUrl, request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
