using eiibd26.Services.Glossary;
using eiibd26.Services.Glossary.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages.Glosario
{
    // AUDITORÍA: Glosario/Index
    // Handler: OnGetAsync -> carga datos para la vista Index.cshtml
    // ViewModel: GlossaryHomeDto (Home), List<GlossaryTermSummaryDto> TopSymptoms, TopTreatments
    // Observado: Conteos y nivel de relación estaban obteniéndose desde el servicio IGlossaryService.
    // Cambios realizados en Services/Glossary/GlossaryService.cs:
    // - GetGlossaryHomeAsync ahora cuenta desde tablas sintomas/tratamientos excluyendo Eliminado
    // - GetTopTermsByQualityAsync rellena UserRelationCount (usuarios distintos) y NivelRelacionEII
    // Nota: No se modificó la estructura de la DB ni modelos EF.
    public class IndexModel : PageModel
    {
        private readonly IGlossaryService _glossaryService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IGlossaryService glossaryService,
            ILogger<IndexModel> logger)
        {
            _glossaryService = glossaryService;
            _logger = logger;
        }

        public GlossaryHomeDto Home { get; set; } = new();
        public List<GlossaryTermSummaryDto> TopSymptoms { get; set; } = new();
        public List<GlossaryTermSummaryDto> TopTreatments { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                Home = await _glossaryService.GetGlossaryHomeAsync();
                // Load top lists (quality filtered)
                TopSymptoms = await _glossaryService.GetTopTermsByQualityAsync(Models.Glossary.GlossaryTermType.Sintoma, 20);
                TopTreatments = await _glossaryService.GetTopTermsByQualityAsync(Models.Glossary.GlossaryTermType.Tratamiento, 20);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de inicio del glosario");
            }
        }
    }
}
