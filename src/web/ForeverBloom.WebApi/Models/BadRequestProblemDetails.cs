using System.Text.Json.Serialization;

namespace ForeverBloom.WebApi.Models;

public sealed class BadRequestProblemDetails : ProblemDetails
{
    [JsonPropertyName("errors")]
    public ErrorDetail[] Errors { get; set; }

    public BadRequestProblemDetails() : this([]) { }

    public BadRequestProblemDetails(ErrorDetail[] errors)
    {
        Title = "One or more errors occurred.";
        Errors = errors;
    }
}
