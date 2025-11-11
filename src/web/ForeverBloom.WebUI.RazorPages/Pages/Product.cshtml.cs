using System.Net;
using ForeverBloom.WebApi.Client;
using ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;
using ForeverBloom.WebUI.RazorPages.PageModels;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebUI.RazorPages.Pages;

public class ProductPageModel : BasePageModel
{
    private readonly ILogger<ProductPageModel> _logger;
    private readonly IApiClient _apiClient;

    public ProductPageModel(ILogger<ProductPageModel> logger, IApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
    }

    [FromRoute]
    public string Slug { get; set; } = string.Empty;

    public GetProductBySlugResponse? Product { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Slug))
        {
            return NotFound();
        }

        try
        {
            var getProductResponse = await _apiClient.Products.GetProductBySlugAsync(Slug);
            switch (getProductResponse.IsSuccess, getProductResponse.StatusCode)
            {
                case (true, _):
                    Product = getProductResponse.Value;
                    break;
                case (false, HttpStatusCode.MovedPermanently):
                    // Extract the new slug from the redirect URL and perform a permanent redirect
                    var newSlug = ExtractSlugFromUrl(getProductResponse.Location!);
                    if (string.IsNullOrWhiteSpace(newSlug))
                    {
                        ErrorMessage = "Wystąpił błąd podczas ładowania strony. Odśwież stronę, lub spróbuj ponownie później.";
                        return Page();
                    }
                    return RedirectPermanent($"/produkt/{newSlug}");
                case (false, HttpStatusCode.NotFound):
                    return NotFound();
                case (false, _):
                    ErrorMessage = "Wystąpił błąd podczas ładowania strony. Odśwież stronę, lub spróbuj ponownie później.";
                    return Page();
            }

            PageTitle = $"{Product!.SeoTitle}";
            PageDescription = Product.MetaDescription;

            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Wystąpił błąd podczas ładowania strony. Odśwież stronę, lub spróbuj ponownie później.";
            _logger.LogError(ex, "Error occurred while loading product page for slug '{Slug}'", Slug);
            return Page();
        }
    }

    private static string? ExtractSlugFromUrl(string redirectUrl)
    {
        // Split by '/' and get the last segment
        var segments = redirectUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[^1] : null;
    }
}
