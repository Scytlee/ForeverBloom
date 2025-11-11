using System.Net;
using ForeverBloom.WebApi.Client;
using ForeverBloom.WebApi.Client.Endpoints.Categories.Contracts;
using ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;
using ForeverBloom.WebUI.RazorPages.PageModels;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebUI.RazorPages.Pages;

public class CategoryPageModel : BasePageModel
{
    private readonly ILogger<CategoryPageModel> _logger;
    private readonly IApiClient _apiClient;

    public CategoryPageModel(ILogger<CategoryPageModel> logger, IApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
    }

    [FromRoute]
    public string Slug { get; set; } = string.Empty;

    [FromRoute]
    public int? PageNumber { get; set; }

    [FromQuery]
    public string? Sort { get; set; }

    public GetCategoryBySlugResponse? Category { get; set; }
    public BrowseCatalogCategoryTreeResponse? CategoryTree { get; set; }
    public BrowseCatalogProductsResponse? Products { get; set; }

    public string? ErrorMessage { get; set; }

    // Available sorting options
    public readonly Dictionary<string, string> SortingOptions = new()
    {
        { "", "domyślne" },
        { "name_asc", "po nazwie (od A do Z)" },
        { "name_desc", "po nazwie (od Z do A)" },
        { "price_asc", "od najtańszych" },
        { "price_desc", "od najdroższych" }
    };

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Slug))
        {
            return NotFound();
        }

        try
        {
            // Normalize sorting: treat empty as null to avoid ?sort=
            if (string.IsNullOrWhiteSpace(Sort)) Sort = null;

            // Try fetch category first before calling other endpoints
            var getCategoryResponse = await _apiClient.Categories.GetCategoryBySlugAsync(Slug);
            switch (getCategoryResponse.IsSuccess, getCategoryResponse.StatusCode)
            {
                case (true, _):
                    Category = getCategoryResponse.Value;
                    break;
                case (false, HttpStatusCode.MovedPermanently):
                    // Extract the new slug from the redirect URL and perform a permanent redirect
                    var newSlug = ExtractSlugFromUrl(getCategoryResponse.Location!);
                    if (string.IsNullOrWhiteSpace(newSlug))
                    {
                        ErrorMessage = "Wystąpił błąd podczas ładowania strony. Odśwież stronę, lub spróbuj ponownie później.";
                        return Page();
                    }
                    return RedirectPermanent($"/kategoria/{newSlug}");
                case (false, HttpStatusCode.NotFound):
                    return NotFound();
                case (false, _):
                    ErrorMessage = "Wystąpił błąd podczas ładowania strony. Odśwież stronę, lub spróbuj ponownie później.";
                    return Page();
            }

            // Category is 100% fetched here

            var browseProductsRequest = new BrowseCatalogProductsRequest
            {
                CategoryId = Category!.Id,
                PageNumber = PageNumber ?? 1,
                PageSize = 12,
                Sort = Sort
            };
            var browseProductsTask = _apiClient.Products.BrowseCatalogProductsAsync(browseProductsRequest);
            // Fetch full category tree (forest) from root (no args)
            var categoryTreeTask = _apiClient.Categories.BrowseCatalogCategoryTreeAsync();
            await Task.WhenAll(browseProductsTask, categoryTreeTask);
            var browseProductsResponse = browseProductsTask.Result;
            var categoryTreeResponse = categoryTreeTask.Result;

            if (!browseProductsResponse.IsSuccess)
            {
                ErrorMessage = "Wystąpił błąd podczas ładowania strony. Odśwież stronę, lub spróbuj ponownie później.";
                return Page();
            }
            if (!categoryTreeResponse.IsSuccess)
            {
                ErrorMessage = "Wystąpił błąd podczas ładowania strony. Odśwież stronę, lub spróbuj ponownie później.";
                return Page();
            }

            Products = browseProductsResponse.Value;
            CategoryTree = categoryTreeResponse.Value;
            PageTitle = Category.Name;
            PageDescription = Category.Description;
            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Wystąpił błąd podczas ładowania strony. Odśwież stronę, lub spróbuj ponownie później.";
            _logger.LogError(ex, "Error occurred while loading category page for slug '{Slug}'", Slug);
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
