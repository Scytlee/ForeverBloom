using ForeverBloom.Frontend.RazorPages.PageModels;

namespace ForeverBloom.Frontend.RazorPages.Pages;

public class PrivacyPageModel : BasePageModel
{
    public void OnGet()
    {
        PageTitle = "Polityka prywatności";
        PageDescription = "Poznaj zasady przetwarzania danych osobowych w Forever Bloom Studio.";
    }
}
