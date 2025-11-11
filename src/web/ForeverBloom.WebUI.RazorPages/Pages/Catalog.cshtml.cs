using ForeverBloom.WebApi.Client;
using ForeverBloom.WebApi.Client.Endpoints.Categories.Contracts;
using ForeverBloom.WebUI.RazorPages.PageModels;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebUI.RazorPages.Pages;

public class CatalogPageModel : BasePageModel
{
    private readonly ILogger<CatalogPageModel> _logger;
    private readonly IApiClient _apiClient;

    public CatalogPageModel(ILogger<CatalogPageModel> logger, IApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
    }

    public BrowseCatalogCategoryTreeResponse? CategoryTree { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        PageTitle = "Katalog";
        PageDescription = "Przeglądaj nasze produkty - kompozycje z suszonych kwiatów, zestawy DIY i wiele więcej.";

        try
        {
            var categoryTreeRequest = new BrowseCatalogCategoryTreeRequest { Depth = 1 };
            var categoryTreeResponse = await _apiClient.Categories.BrowseCatalogCategoryTreeAsync(categoryTreeRequest);
            if (!categoryTreeResponse.IsSuccess)
            {
                ErrorMessage = "Wystąpił błąd podczas ładowania katalogu. Odśwież stronę, lub spróbuj ponownie później.";
                _logger.LogWarning("Failed to load category tree for catalog page: {Error}", categoryTreeResponse.Error?.Message);
                return Page();
            }

            CategoryTree = categoryTreeResponse.Value;
            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Wystąpił błąd podczas ładowania katalogu. Odśwież stronę, lub spróbuj ponownie później.";
            _logger.LogError(ex, "Error occurred while loading catalog page");
            return Page();
        }
    }
}
