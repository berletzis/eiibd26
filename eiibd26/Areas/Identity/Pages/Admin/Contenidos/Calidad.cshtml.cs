using eiibd26.Services.Calidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    [Authorize(Roles = "Administrador")]
    public class CalidadModel : PageModel
    {
        private readonly IContenidoCalidadService _calidad;

        public CalidadModel(IContenidoCalidadService calidad)
            => _calidad = calidad;

        public List<ContenidoCalidadDto>? Resultados { get; private set; }
        public bool Analizado { get; private set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAnalizarAsync()
        {
            Resultados = await _calidad.AnalizarTodosAsync();
            Analizado = true;
            return Page();
        }
    }
}
