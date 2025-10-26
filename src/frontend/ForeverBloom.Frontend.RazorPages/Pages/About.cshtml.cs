using ForeverBloom.Frontend.RazorPages.PageModels;

namespace ForeverBloom.Frontend.RazorPages.Pages;

public class AboutPageModel : BasePageModel
{
    public void OnGet()
    {
        PageTitle = "O nas";
        PageDescription = "Poznaj historię Forever Bloom Studio i naszą pasję do tworzenia wyjątkowych kompozycji z suszonych kwiatów.";
    }
}
