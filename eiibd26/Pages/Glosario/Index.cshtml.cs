using eiibd26.Services.Glossary;
using eiibd26.Services.Glossary.DTOs;
using eiibd26.Services.Validacion;
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
        private readonly IValidacionContenidoService _validacionSvc;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IGlossaryService glossaryService,
            IValidacionContenidoService validacionSvc,
            ILogger<IndexModel> logger)
        {
            _glossaryService = glossaryService;
            _validacionSvc = validacionSvc;
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

                // Batch-load validators: una sola query para ambas listas combinadas
                var allIds = TopSymptoms.Select(t => t.Id)
                    .Union(TopTreatments.Select(t => t.Id)).ToList();
                var validadoresDict = await _validacionSvc.ObtenerValidadoresGlosarioPorTerminosAsync(allIds);
                foreach (var t in TopSymptoms)
                    if (validadoresDict.TryGetValue(t.Id, out var v)) t.Validadores = v;
                foreach (var t in TopTreatments)
                    if (validadoresDict.TryGetValue(t.Id, out var v)) t.Validadores = v;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de inicio del glosario");
            }
        }
    }
}
