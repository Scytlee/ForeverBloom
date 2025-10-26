using ForeverBloom.Frontend.RazorPages.PageModels;

namespace ForeverBloom.Frontend.RazorPages.Pages;

public class BouquetCarePageModel : BasePageModel
{
    public void OnGet()
    {
        PageTitle = "Konserwacja bukietów okolicznościowych";
        PageDescription = "Dowiedz się więcej o naszej usłudze konserwacji bukietów okolicznościowych. Ślub, rocznica.";
    }
}
