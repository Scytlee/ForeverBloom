using System.Text.Json.Serialization;
using ForeverBloom.WebApi.Client.Contracts;

namespace ForeverBloom.WebApi.Client.Endpoints.Internal;

internal sealed class ValidationProblemDetails : ProblemDetails
{
    [JsonPropertyName("errors")]
    public IDictionary<string, ValidationErrorDetail[]>? Errors { get; set; }
}
