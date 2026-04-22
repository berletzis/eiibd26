using eiibd26.Models.Glossary;
using eiibd26.Services.Glossary;
using eiibd26.Services.Glossary.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages.Glosario
{
    public class TratamientosModel : PageModel
    {
        private readonly IGlossaryService _glossaryService;
        private readonly ILogger<TratamientosModel> _logger;

        public TratamientosModel(
            IGlossaryService glossaryService,
            ILogger<TratamientosModel> logger)
        {
            _glossaryService = glossaryService;
            _logger = logger;
        }

        public List<GlossaryTermDto> Terminos { get; set; } = new();

        /// <summary>Filtro de nivel de relación con EII (null = todos)</summary>
        [BindProperty(SupportsGet = true)]
        public MedicalRelationType? NivelFiltro { get; set; }

        /// <summary>
        /// Términos filtrados por nivel EII y agrupados por letra inicial
        /// </summary>
        public IEnumerable<IGrouping<char, GlossaryTermDto>> TerminosAgrupados =>
            (NivelFiltro.HasValue
                ? Terminos.Where(t => t.NivelRelacion == NivelFiltro)
                : Terminos)
            .GroupBy(t => t.LetraInicial)
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
                Terminos = await _glossaryService.GetTermsByTypeAsync(GlossaryTermType.Tratamiento);
                _logger.LogInformation("Página de tratamientos cargada con {Count} términos", Terminos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de tratamientos");
            }
        }
    }
}
