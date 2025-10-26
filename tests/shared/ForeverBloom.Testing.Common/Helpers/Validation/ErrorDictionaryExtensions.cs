using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;

namespace ForeverBloom.Testing.Common.Helpers.Validation;

public static class ErrorDictionaryExtensions
{
    public static async Task<IDictionary<string, string[]>> ExtractErrorDictionaryAsync(this HttpResponseMessage response,
      CancellationToken cancellationToken = default)
    {
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonNode = JsonNode.Parse(jsonString);
        jsonNode.Should().NotBeNull();

        var errorsNode = jsonNode["errors"];
        errorsNode.Should().NotBeNull();

        var errorDictionary = errorsNode.Deserialize<Dictionary<string, string[]>>();
        errorDictionary.Should().NotBeNull();
        return errorDictionary;
    }

    public static void ShouldContainErrorForProperty(this IDictionary<string, string[]> errorDictionary,
      string propertyName, string expectedErrorCode)
    {
        var propertyErrorsExist = errorDictionary.TryGetValue(propertyName, out var propertyErrors);
        propertyErrorsExist.Should().BeTrue();
        propertyErrors.Should().NotBeNull();
        propertyErrors.Should().Contain(expectedErrorCode);
    }

    public static void ShouldOnlyContainErrorsForProperty(this IDictionary<string, string[]> errorDictionary,
      string propertyName)
    {
        errorDictionary.Should().HaveCount(1);
        var propertyErrorsExist = errorDictionary.TryGetValue(propertyName, out var propertyErrors);
        propertyErrorsExist.Should().BeTrue();
        propertyErrors.Should().NotBeNull();
    }
}
