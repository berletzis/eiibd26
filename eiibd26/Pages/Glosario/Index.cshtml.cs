using eiibd26.Services.Glossary;
using eiibd26.Services.Glossary.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages.Glosario
{
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
