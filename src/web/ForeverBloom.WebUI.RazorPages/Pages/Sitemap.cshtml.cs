using System.Globalization;
using System.Text;
using System.Xml;
using ForeverBloom.WebApi.Client;
using ForeverBloom.WebApi.Client.Contracts;
using ForeverBloom.WebApi.Client.Endpoints.Categories.Contracts;
using ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ForeverBloom.WebUI.RazorPages.Pages;

public class SitemapPageModel : PageModel
{
    private readonly ILogger<SitemapPageModel> _logger;
    private readonly IApiClient _apiClient;
    private readonly IWebHostEnvironment _environment;

    public SitemapPageModel(ILogger<SitemapPageModel> logger, IApiClient apiClient, IWebHostEnvironment environment)
    {
        _logger = logger;
        _apiClient = apiClient;
        _environment = environment;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        // if (!_environment.IsProduction())
        // {
        //     return NotFound();
        // }
        try
        {
            var xml = await GenerateSitemapXmlAsync(cancellationToken);
            return Content(xml, "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating sitemap");
            return StatusCode(500);
        }
    }

    private async Task<string> GenerateSitemapXmlAsync(CancellationToken cancellationToken)
    {
        var baseUrl = GetBaseUrl();

        // The settings are correct.
        var settings = new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            Encoding = Encoding.UTF8 // This will now be respected
        };

        // Use a MemoryStream instead of a StringBuilder.
        await using (var memoryStream = new MemoryStream())
        {
            await using (var xmlWriter = XmlWriter.Create(memoryStream, settings))
            {
                await xmlWriter.WriteStartDocumentAsync();
                xmlWriter.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                // Add static pages
                AddStaticUrls(xmlWriter, baseUrl);

                // Add categories and products
                var categoriesTask = _apiClient.Categories.GetCategoriesSitemapDataAsync(cancellationToken);
                var productsTask = _apiClient.Products.GetProductsSitemapDataAsync(cancellationToken);

                await Task.WhenAll(categoriesTask, productsTask);

                ProcessCategoriesResponse(categoriesTask.Result, xmlWriter, baseUrl);
                ProcessProductsResponse(productsTask.Result, xmlWriter, baseUrl);

                await xmlWriter.WriteEndElementAsync(); // urlset
                await xmlWriter.WriteEndDocumentAsync();
                await xmlWriter.FlushAsync();
            }

            // After writing, read the stream back into a string.
            memoryStream.Position = 0; // Rewind the stream to the beginning
            using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
            {
                return await streamReader.ReadToEndAsync(cancellationToken);
            }
        }
    }

    private void AddStaticUrls(XmlWriter xmlWriter, string baseUrl)
    {
        var staticLastMod = new DateOnly(2025, 9, 17);
        var privacyLastMod = new DateOnly(2025, 9, 17);

        var staticPages = new[]
        {
            new { Url = "/", LastMod = staticLastMod },
            new { Url = "/katalog", LastMod = staticLastMod },
            new { Url = "/o-nas", LastMod = staticLastMod },
            new { Url = "/konserwacja-bukietow-okolicznosciowych", LastMod = staticLastMod },
            new { Url = "/kontakt", LastMod = staticLastMod },
            new { Url = "/polityka-prywatnosci", LastMod = privacyLastMod }
        };

        foreach (var page in staticPages)
        {
            AddUrlElement(xmlWriter, baseUrl, page.Url, page.LastMod);
        }
    }

    private void ProcessCategoriesResponse(HttpResult<GetCategoriesSitemapDataResponse> categoriesResponse, XmlWriter xmlWriter, string baseUrl)
    {
        if (categoriesResponse.IsSuccess)
        {
            foreach (var category in categoriesResponse.Value!.Items)
            {
                AddUrlElement(xmlWriter, baseUrl, $"/kategoria/{category.Slug}", DateOnly.FromDateTime(category.UpdatedAt.Date));
            }
        }
        else
        {
            _logger.LogWarning("Failed to load categories for sitemap: {Error}", categoriesResponse.Error!.Message);
            throw new HttpRequestException($"Failed to load categories for sitemap: {categoriesResponse.Error.Message}");
        }
    }

    private void ProcessProductsResponse(HttpResult<GetProductsSitemapDataResponse> productsResponse, XmlWriter xmlWriter, string baseUrl)
    {
        if (productsResponse.IsSuccess)
        {
            foreach (var product in productsResponse.Value!.Items)
            {
                AddUrlElement(xmlWriter, baseUrl, $"/produkt/{product.Slug}", DateOnly.FromDateTime(product.UpdatedAt.Date));
            }
        }
        else
        {
            _logger.LogWarning("Failed to load products for sitemap: {Error}", productsResponse.Error!.Message);
            throw new HttpRequestException($"Failed to load products for sitemap: {productsResponse.Error.Message}");
        }
    }

    private static void AddUrlElement(XmlWriter xmlWriter, string baseUrl, string relativeUrl, DateOnly lastMod)
    {
        xmlWriter.WriteStartElement("url");

        xmlWriter.WriteElementString("loc", $"{baseUrl.TrimEnd('/')}{relativeUrl}");
        xmlWriter.WriteElementString("lastmod", lastMod.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        xmlWriter.WriteEndElement(); // url
    }

    private string GetBaseUrl()
    {
        if (_environment.IsProduction())
        {
            return "https://foreverbloomstudio.pl";
        }

        // For development/staging environments, use the request base URL
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}";
    }
}
