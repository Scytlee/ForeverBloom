using ForeverBloom.WebUI.RazorPages.PageModels;

namespace ForeverBloom.WebUI.RazorPages.Pages;

public class PrivacyPageModel : BasePageModel
{
    public void OnGet()
    {
        PageTitle = "Polityka prywatności";
        PageDescription = "Poznaj zasady przetwarzania danych osobowych w Forever Bloom Studio.";
    }
}
