namespace ForeverBloom.Api.Tests.Extensions;

public static class ApiKeyClientExtensions
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const string AdminKey = "AdminKey";
    private const string FrontendKey = "FrontendKey";

    public static HttpClient UseAdminKey(this HttpClient client)
    {
        if (client.DefaultRequestHeaders.Contains(ApiKeyHeader))
            client.DefaultRequestHeaders.Remove(ApiKeyHeader);
        client.DefaultRequestHeaders.Add(ApiKeyHeader, AdminKey);
        return client;
    }

    public static HttpClient UseFrontendKey(this HttpClient client)
    {
        if (client.DefaultRequestHeaders.Contains(ApiKeyHeader))
            client.DefaultRequestHeaders.Remove(ApiKeyHeader);
        client.DefaultRequestHeaders.Add(ApiKeyHeader, FrontendKey);
        return client;
    }

    public static HttpClient UseFrontendKey(this HttpClient client, string key)
    {
        if (client.DefaultRequestHeaders.Contains(ApiKeyHeader))
            client.DefaultRequestHeaders.Remove(ApiKeyHeader);
        client.DefaultRequestHeaders.Add(ApiKeyHeader, key);
        return client;
    }
}
