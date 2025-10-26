using ForeverBloom.Frontend.RazorPages.PageModels;

namespace ForeverBloom.Frontend.RazorPages.Pages;

public class ContactPageModel : BasePageModel
{
    public void OnGet()
    {
        PageTitle = "Kontakt";
        PageDescription = "Skontaktuj się z Forever Bloom Studio. Zapraszamy do współpracy i odpowiadamy na wszystkie pytania dotyczące naszych produktów.";
    }
}
