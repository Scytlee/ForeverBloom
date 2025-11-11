using System.Text.Json.Serialization;

namespace ForeverBloom.WebApi.Client.Contracts;

public sealed class ErrorDetail
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; init; }
}
