using ForeverBloom.ApiClient;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.ListProducts;
using ForeverBloom.Frontend.RazorPages.PageModels;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.Frontend.RazorPages.Pages;

public class IndexPageModel : BasePageModel
{
    private readonly ILogger<IndexPageModel> _logger;
    private readonly IApiClient _apiClient;

    public IndexPageModel(ILogger<IndexPageModel> logger, IApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
    }

    public ListProductsResponse? FeaturedProducts { get; set; }
    public bool ShowFeaturedProducts => FeaturedProducts?.Items.Any() ?? false;

    public async Task<IActionResult> OnGetAsync()
    {
        PageTitle = "Obrazy z suszonych kwiatów";
        PageDescription =
            "Ręcznie tworzone obrazy z suszonych roślin oraz profesjonalna konserwacja bukietów ślubnych " +
            "i okolicznościowych w ramach 3D. Naturalne piękno, które zostaje na dłużej.";

        try
        {
            var featuredProductsRequest = new ListProductsRequest { Featured = true, PageSize = 10, PageNumber = 1 };
            var featuredProductsResponse = await _apiClient.Products.ListProductsAsync(featuredProductsRequest);

            if (featuredProductsResponse.IsSuccess)
            {
                FeaturedProducts = featuredProductsResponse.Value;
            }
            else
            {
                _logger.LogWarning("Failed to load featured products for homepage: {Error}",
                    featuredProductsResponse.Error?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading featured products for homepage");
        }

        return Page();
    }
}
