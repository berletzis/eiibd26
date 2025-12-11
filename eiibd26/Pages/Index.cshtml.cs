using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Redirige a la página creada en /Home (Pages/Home/Index.cshtml)
            return RedirectToPage("/Home/Index");
        }
    }
}