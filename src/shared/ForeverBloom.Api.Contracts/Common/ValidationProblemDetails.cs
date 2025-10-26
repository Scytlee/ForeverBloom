using System.Text.Json.Serialization;

namespace ForeverBloom.Api.Contracts.Common;

public sealed class ValidationProblemDetails : ProblemDetails
{
    [JsonPropertyName("errors")]
    public IDictionary<string, string[]> Errors { get; set; }

    public ValidationProblemDetails() : this(new Dictionary<string, string[]>()) { }

    public ValidationProblemDetails(IDictionary<string, string[]> errors)
    {
        Title = "One or more validation errors occurred.";
        Errors = errors;
    }
}
