using eiibd26.Services.Glossary;
using eiibd26.Services.Glossary.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages.Glosario
{
    public class TerminoModel : PageModel
    {
        private readonly IGlossaryService _glossaryService;
        private readonly ILogger<TerminoModel> _logger;

        public TerminoModel(
            IGlossaryService glossaryService,
            ILogger<TerminoModel> logger)
        {
            _glossaryService = glossaryService;
            _logger = logger;
        }

        public GlossaryTermDetailDto? Term { get; set; }

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            try
            {
                Term = await _glossaryService.GetTermBySlugAsync(slug);

                if (Term == null)
                {
                    _logger.LogWarning("Término con slug '{Slug}' no encontrado", slug);
                    return NotFound();
                }

                _logger.LogInformation("Término '{Nombre}' cargado exitosamente", Term.Nombre);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar término con slug '{Slug}'", slug);
                return NotFound();
            }
        }
    }
}
