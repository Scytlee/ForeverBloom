using System.Text.Json.Serialization;

namespace ForeverBloom.WebApi.Models;

public sealed class ValidationProblemDetails : ProblemDetails
{
    [JsonPropertyName("errors")]
    public IDictionary<string, ValidationErrorDetail[]> Errors { get; set; }

    public ValidationProblemDetails() : this(new Dictionary<string, ValidationErrorDetail[]>()) { }

    public ValidationProblemDetails(IDictionary<string, ValidationErrorDetail[]> errors)
    {
        Title = "One or more validation errors occurred.";
        Errors = errors;
    }
}
