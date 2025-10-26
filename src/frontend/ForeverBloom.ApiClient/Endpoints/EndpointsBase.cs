using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.ApiClient.Contracts;
using Microsoft.Extensions.Logging;

namespace ForeverBloom.ApiClient.Endpoints;

internal abstract class EndpointsBase<T> where T : class
{
    protected readonly ILogger<T> Logger;

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected EndpointsBase(HttpClient httpClient, ILogger<T> logger)
    {
        _httpClient = httpClient;
        Logger = logger;
    }

    private async Task<ApiResponse<TResponse>> HandleResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken = default) where TResponse : notnull
    {
        // 1. Handle Success (2xx)
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadFromJsonAsync<TResponse>(_serializerOptions, cancellationToken);
            return responseContent is null
              ? ApiResponse<TResponse>.Failure(new Error("Api.NullResponse", "API returned a success status code but the response body was empty or null."), response.StatusCode)
              : ApiResponse<TResponse>.Success(responseContent, response.StatusCode);
        }

        // 2. Handle Redirects (3xx) - THIS IS THE NEW LOGIC
        if (response.StatusCode is >= HttpStatusCode.Moved and < HttpStatusCode.BadRequest)
        {
            var location = response.Headers.Location;
            return location is not null
              // Successfully captured the redirect information
              ? ApiResponse<TResponse>.Failure(new Error("Api.Redirect", "API returned a redirect status code."), response.StatusCode, location: location.ToString())
              // A 3xx response without a Location header is an error condition
              : ApiResponse<TResponse>.Failure(new Error($"Api.Http.{(int)response.StatusCode}", "Redirect response received without a Location header."), response.StatusCode);
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // Handle the two distinct failure cases: Validation (400) vs. all other errors
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            // Case 1: Handle ValidationProblemDetails for 400 Bad Request
            try
            {
                var validationProblem = JsonSerializer.Deserialize<ValidationProblemDetails>(errorContent, _serializerOptions);
                if (validationProblem is not null)
                {
                    return ApiResponse<TResponse>.Failure(new ValidationError(validationProblem.Errors), response.StatusCode);
                }
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Could not deserialize a 400 Bad Request response as ValidationProblemDetails. Content: {Content}", errorContent);
            }
        }

        // Case 2: Handle standard ProblemDetails for all other 4xx/5xx errors.
        try
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(errorContent, _serializerOptions);
            if (problemDetails is not null)
            {
                var errorCode = $"Api.Http.{(int)response.StatusCode}";
                var errorMessage = problemDetails.Detail ?? problemDetails.Title ?? "An API error occurred";
                Logger.LogWarning("API call failed with status code {StatusCode}: {ErrorMessage}. TraceId: {TraceId}", (int)response.StatusCode, errorMessage, problemDetails.Extensions["traceId"]);
                return ApiResponse<TResponse>.Failure(new Error(errorCode, errorMessage), response.StatusCode);
            }
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "Could not deserialize the error response as ProblemDetails. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
        }

        // Fallback: If the body is empty or not JSON, create an error from the status code.
        var fallbackErrorCode = $"Api.Http.{(int)response.StatusCode}";
        var fallbackErrorMessage = $"API call failed with status code {response.StatusCode} ({response.ReasonPhrase})";
        Logger.LogWarning("API call failed with status code {StatusCode} ({ReasonPhrase})", response.StatusCode, response.ReasonPhrase);
        return ApiResponse<TResponse>.Failure(new Error(fallbackErrorCode, fallbackErrorMessage), response.StatusCode);
    }

    protected async Task<ApiResponse<TResponse>> GetJsonAsync<TResponse>(Uri? requestUri, CancellationToken cancellationToken = default) where TResponse : notnull
    {
        try
        {
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            return await HandleResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed for {RequestUri}", requestUri);
            return ApiResponse<TResponse>.Failure(new Error("Api.NetworkError", "A network error occurred while communicating with the API.", ex), HttpStatusCode.ServiceUnavailable);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred during GET request to {RequestUri}", requestUri);
            return ApiResponse<TResponse>.Failure(new Error("General.Exception", ex.Message, ex), HttpStatusCode.ServiceUnavailable);
        }
    }
}
