using System.Text.Json.Serialization;
using ForeverBloom.WebApi.Client.Contracts;

namespace ForeverBloom.WebApi.Client.Endpoints.Internal;

internal sealed class BadRequestProblemDetails : ProblemDetails
{
    [JsonPropertyName("errors")]
    public ErrorDetail[]? Errors { get; set; }
}
