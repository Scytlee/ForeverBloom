using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ForeverBloom.Api.Authentication.Settings;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Api.Results;

namespace ForeverBloom.Api.Authentication;

public static class ApiKeyAuthenticationDefaults
{
    public const string SchemeName = "ApiKey";

    public const string AdminAccessPolicyName = "AdminAccess";
    public const string FrontendAccessPolicyName = "FrontendAccess";

    public const string AdminScope = "admin-api-access";
    public const string FrontendScope = "frontend-api-access";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApiKeyAuthenticationSettings _settings;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> authenticationSchemeOptionsMonitor,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiKeyAuthenticationSettings> settings) : base(authenticationSchemeOptionsMonitor, logger, encoder)
    {
        _settings = settings.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(_settings.HeaderName, out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = headerValues.FirstOrDefault();
        if (headerValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsPrincipal principal;
        if (providedApiKey == _settings.AdminKey)
        {
            var adminClaims = new[] { new Claim("Scope", ApiKeyAuthenticationDefaults.AdminScope) };
            var adminIdentity = new ClaimsIdentity(adminClaims, Scheme.Name);
            principal = new ClaimsPrincipal(adminIdentity);
        }
        else if (_settings.FrontendKeys.Contains(providedApiKey))
        {
            var frontendClaims = new[] { new Claim("Scope", ApiKeyAuthenticationDefaults.FrontendScope) };
            var frontendIdentity = new ClaimsIdentity(frontendClaims, Scheme.Name);
            principal = new ClaimsPrincipal(frontendIdentity);
        }
        else
        {
            Logger.LogWarning("Invalid API Key provided.");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided."));
        }

        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (!Response.HasStarted)
        {
            Response.Headers.WWWAuthenticate = ApiKeyAuthenticationDefaults.SchemeName;

            var unauthorizedResult = ApiResults.Unauthorized();
            ProblemDetailsHelper.EnrichProblemDetails(unauthorizedResult.Value, Context);
            await unauthorizedResult.ExecuteAsync(Context);
        }
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        if (!Response.HasStarted)
        {
            var forbiddenResult = ApiResults.Forbidden();
            ProblemDetailsHelper.EnrichProblemDetails(forbiddenResult.Value, Context);
            await forbiddenResult.ExecuteAsync(Context);
        }
    }
}
