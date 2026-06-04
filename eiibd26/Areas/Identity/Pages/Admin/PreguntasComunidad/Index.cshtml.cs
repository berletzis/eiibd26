using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Areas.Identity.Pages.Admin.PreguntasComunidad
{
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        public void OnGet() { }
    }
}
