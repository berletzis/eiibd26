using eiibd26.Models.Glossary;
using eiibd26.Services.Glossary;
using eiibd26.Services.Glossary.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages.Glosario
{
    public class SintomasModel : PageModel
    {
        private readonly IGlossaryService _glossaryService;
        private readonly ILogger<SintomasModel> _logger;

        public SintomasModel(
            IGlossaryService glossaryService,
            ILogger<SintomasModel> logger)
        {
            _glossaryService = glossaryService;
            _logger = logger;
        }

        public List<GlossaryTermDto> Terminos { get; set; } = new();
        
        /// <summary>
        /// Términos agrupados por letra inicial
        /// </summary>
        public IEnumerable<IGrouping<char, GlossaryTermDto>> TerminosAgrupados =>
            Terminos.GroupBy(t => t.LetraInicial)
                   .OrderBy(g => g.Key);

        /// <summary>
        /// Letras que tienen términos (para navegación)
        /// </summary>
        public List<char> LetrasDisponibles =>
            TerminosAgrupados.Select(g => g.Key).ToList();

        public async Task OnGetAsync()
        {
            try
            {
                Terminos = await _glossaryService.GetTermsByTypeAsync(GlossaryTermType.Sintoma);
                _logger.LogInformation("Página de síntomas cargada con {Count} términos", Terminos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de síntomas");
            }
        }
    }
}
