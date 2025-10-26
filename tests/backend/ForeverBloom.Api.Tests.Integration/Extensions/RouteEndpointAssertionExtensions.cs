using FluentAssertions;
using ForeverBloom.Api.EndpointFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace ForeverBloom.Api.Tests.Extensions;

/// <summary>
/// FluentAssertions extensions for validating RouteEndpoint metadata in integration tests.
/// Provides readable assertion methods for endpoint names, tags, HTTP verbs, routes, and filters.
/// </summary>
public static class RouteEndpointAssertionExtensions
{
    /// <summary>
    /// Asserts that the endpoint has the specified name.
    /// </summary>
    /// <param name="endpoint">The endpoint to validate</param>
    /// <param name="expectedName">The expected endpoint name</param>
    public static void ShouldHaveName(this RouteEndpoint endpoint, string expectedName)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedName);

        var nameMetadata = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
        var actualName = nameMetadata?.EndpointName;

        actualName.Should().Be(expectedName);
    }

    /// <summary>
    /// Asserts that the endpoint has the specified tag.
    /// </summary>
    /// <param name="endpoint">The endpoint to validate</param>
    /// <param name="expectedTag">The expected tag</param>
    public static void ShouldHaveTag(this RouteEndpoint endpoint, string expectedTag)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedTag);

        var tags = endpoint.Metadata.OfType<ITagsMetadata>().FirstOrDefault()?.Tags ?? [];

        tags.Should().Contain(expectedTag);
    }

    /// <summary>
    /// Asserts that the endpoint supports the specified HTTP verb.
    /// </summary>
    /// <param name="endpoint">The endpoint to validate</param>
    /// <param name="expectedVerb">The expected HTTP verb (e.g., "GET", "POST")</param>
    public static void ShouldHaveHttpVerb(this RouteEndpoint endpoint, string expectedVerb)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedVerb);

        var normalizedExpected = expectedVerb.ToUpperInvariant();
        var httpMethods = endpoint.Metadata.OfType<IHttpMethodMetadata>().FirstOrDefault()?.HttpMethods ?? [];

        httpMethods.Should().Contain(normalizedExpected);
    }

    /// <summary>
    /// Asserts that the endpoint has the specified route prefix.
    /// </summary>
    /// <param name="endpoint">The endpoint to validate</param>
    /// <param name="expectedPrefix">The expected route prefix</param>
    public static void ShouldHaveRoutePrefix(this RouteEndpoint endpoint, string expectedPrefix)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPrefix);

        var actualRoute = endpoint.RoutePattern.RawText;

        actualRoute.Should().StartWith(expectedPrefix);
    }

    /// <summary>
    /// Asserts that the endpoint validates requests of the specified type.
    /// </summary>
    /// <typeparam name="T">The request type that should be validated</typeparam>
    /// <param name="endpoint">The endpoint to validate</param>
    public static void ShouldValidateRequest<T>(this RouteEndpoint endpoint) where T : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var hasValidationMetadata = endpoint.Metadata.OfType<ValidationApplied<T>>().Any();

        hasValidationMetadata.Should().BeTrue();
    }

    /// <summary>
    /// Asserts that the endpoint uses the Unit of Work pattern.
    /// </summary>
    /// <param name="endpoint">The endpoint to validate</param>
    public static void ShouldUseUnitOfWork(this RouteEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var hasUnitOfWorkMetadata = endpoint.Metadata.OfType<UnitOfWorkApplied>().Any();

        hasUnitOfWorkMetadata.Should().BeTrue();
    }

    /// <summary>
    /// Asserts that the endpoint has the specified authorization policy.
    /// </summary>
    /// <param name="endpoint">The endpoint to validate</param>
    /// <param name="expectedPolicy">The expected authorization policy name</param>
    public static void ShouldHaveAuthorizationPolicy(this RouteEndpoint endpoint, string expectedPolicy)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPolicy);

        var authorizeData = endpoint.Metadata.OfType<IAuthorizeData>().ToList();

        authorizeData.Should().NotBeEmpty();
        authorizeData.Should().Contain(data => data.Policy == expectedPolicy);
    }
}
