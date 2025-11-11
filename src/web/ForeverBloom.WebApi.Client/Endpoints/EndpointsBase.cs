using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.WebApi.Client.Contracts;
using ForeverBloom.WebApi.Client.Endpoints.Internal;
using ForeverBloom.WebApi.Client.Serialization;
using Microsoft.Extensions.Logging;

namespace ForeverBloom.WebApi.Client.Endpoints;

internal abstract class EndpointsBase<T> where T : class
{
    protected readonly ILogger<T> Logger;

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new ProductAvailabilityStatusConverter() }
    };

    protected EndpointsBase(HttpClient httpClient, ILogger<T> logger)
    {
        _httpClient = httpClient;
        Logger = logger;
    }

    private async Task<HttpResult<TResponse>> HandleResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken = default) where TResponse : notnull
    {
        // Handle success responses (2xx)
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadFromJsonAsync<TResponse>(_serializerOptions, cancellationToken);
            return responseContent is null
              ? HttpResult<TResponse>.Failure(new Error { Code = "Api.NullResponse", Message = "API returned a success status code but the response body was empty or null." }, response.StatusCode)
              : HttpResult<TResponse>.Success(responseContent, response.StatusCode);
        }

        // Handle redirect responses (3xx)
        if ((int)response.StatusCode is >= 300 and < 400)
        {
            var location = response.Headers.Location;
            return location is not null
              // Successfully captured the redirect information
              ? HttpResult<TResponse>.Failure(new Error { Code = "Api.Redirect", Message = "API returned a redirect status code." }, response.StatusCode, location: location.ToString())
              // A 3xx response without a Location header is an error condition
              : HttpResult<TResponse>.Failure(new Error { Code = $"Api.Http.{(int)response.StatusCode}", Message = "Redirect response received without a Location header." }, response.StatusCode);
        }

        // Handle client error responses (4xx) and server error responses (5xx)
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var jsonDoc = JsonDocument.Parse(errorContent);
            var root = jsonDoc.RootElement;

            // Inspect the "errors" property structure to determine error type
            if (root.TryGetProperty("errors", out var errorsProperty))
            {
                switch (errorsProperty.ValueKind)
                {
                    // Case 1: errors is an object (dictionary) -> ValidationProblemDetails
                    case JsonValueKind.Object:
                        {
                            var validationProblem = root.Deserialize<ValidationProblemDetails>(_serializerOptions);
                            if (validationProblem?.Errors is not null)
                            {
                                return HttpResult<TResponse>.Failure(new ValidationError(validationProblem.Errors), response.StatusCode);
                            }

                            break;
                        }
                    // Case 2: errors is an array -> BadRequestProblemDetails
                    case JsonValueKind.Array:
                        {
                            var badRequestProblem = root.Deserialize<BadRequestProblemDetails>(_serializerOptions);
                            if (badRequestProblem?.Errors is not null)
                            {
                                return HttpResult<TResponse>.Failure(new BadRequestError(badRequestProblem.Errors), response.StatusCode);
                            }

                            break;
                        }
                }
            }

            // Case 3: Standard ProblemDetails (no errors property, or errors property wasn't object/array)
            var problemDetails = root.Deserialize<ProblemDetails>(_serializerOptions);
            if (problemDetails is not null)
            {
                var errorCode = problemDetails.ErrorCode ?? $"Api.Http.{(int)response.StatusCode}";
                var errorMessage = problemDetails.Detail ?? problemDetails.Title ?? "An API error occurred";
                Logger.LogWarning("API call failed with status code {StatusCode}: {ErrorMessage}. RequestId: {RequestId}, TraceId: {TraceId}",
                    (int)response.StatusCode, errorMessage, problemDetails.RequestId, problemDetails.TraceId);
                return HttpResult<TResponse>.Failure(new Error { Code = errorCode, Message = errorMessage }, response.StatusCode);
            }
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "Could not parse error response JSON. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
        }

        // Fallback: If the body is empty or not JSON, create an error from the status code
        var fallbackErrorCode = $"Api.Http.{(int)response.StatusCode}";
        var fallbackErrorMessage = $"API call failed with status code {response.StatusCode} ({response.ReasonPhrase})";
        Logger.LogWarning("API call failed with status code {StatusCode} ({ReasonPhrase})", response.StatusCode, response.ReasonPhrase);
        return HttpResult<TResponse>.Failure(new Error { Code = fallbackErrorCode, Message = fallbackErrorMessage }, response.StatusCode);
    }

    protected async Task<HttpResult<TResponse>> GetJsonAsync<TResponse>(Uri? requestUri, CancellationToken cancellationToken = default) where TResponse : notnull
    {
        try
        {
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            return await HandleResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed for {RequestUri}", requestUri);
            return HttpResult<TResponse>.Failure(
                new ClientError
                {
                    Code = "Api.NetworkError",
                    Message = "A network error occurred while communicating with the API.",
                    Exception = ex
                },
                HttpStatusCode.ServiceUnavailable);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred during GET request to {RequestUri}", requestUri);
            return HttpResult<TResponse>.Failure(
                new ClientError
                {
                    Code = "General.Exception",
                    Message = ex.Message,
                    Exception = ex
                },
                HttpStatusCode.ServiceUnavailable);
        }
    }
}
