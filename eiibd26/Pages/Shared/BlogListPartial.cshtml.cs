using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages.Shared
{
    // Minimal PageModel included as requested; actual model is passed from Index page.
    public class BlogListPartialModel : PageModel
    {
        public void OnGet() { /* partial receives model from Index page */ }
    }
}