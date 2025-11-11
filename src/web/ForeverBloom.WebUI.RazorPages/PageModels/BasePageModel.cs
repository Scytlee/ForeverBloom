using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ForeverBloom.WebUI.RazorPages.PageModels;

public abstract class BasePageModel : PageModel
{
    public string? PageTitle { get; set; }
    public string? PageDescription { get; set; }
}
