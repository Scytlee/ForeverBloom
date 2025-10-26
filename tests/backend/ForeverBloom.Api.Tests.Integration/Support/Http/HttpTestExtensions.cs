namespace ForeverBloom.Api.Tests.Support.Http;

public static class HttpTestExtensions
{
    private const int MaxBodyChars = 4096;

    public static async Task EnsureSuccessWithDiagnosticsAsync(
      this HttpResponseMessage response,
      CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode) return;

        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            body = "<unable to read response body>";
        }

        var method = response.RequestMessage?.Method.Method ?? "UNKNOWN";
        var url = response.RequestMessage?.RequestUri?.ToString() ?? "UNKNOWN";
        var status = (int)response.StatusCode;
        var reason = response.ReasonPhrase ?? response.StatusCode.ToString();
        var truncated = Truncate(body, MaxBodyChars);

        throw new HttpRequestException(
          $"HTTP {method} {url} → {status} {reason}\n{truncated}");
    }

    private static string Truncate(string value, int max) =>
      value.Length <= max ? value : value[..max] + " …[truncated]";
}
