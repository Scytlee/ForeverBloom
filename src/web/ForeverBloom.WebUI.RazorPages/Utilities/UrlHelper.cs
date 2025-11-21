namespace ForeverBloom.WebUI.RazorPages.Utilities;

public static class UrlHelper
{
    public static string? ExtractSlugFromUrl(string redirectUrl)
    {
        var segments = redirectUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[^1] : null;
    }
}
