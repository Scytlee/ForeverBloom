using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ForeverBloom.Frontend.RazorPages.PageModels;

public abstract class BasePageModel : PageModel
{
    public string? PageTitle { get; set; }
    public string? PageDescription { get; set; }
}
