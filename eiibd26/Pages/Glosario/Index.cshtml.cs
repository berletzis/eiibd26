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

        public async Task OnGetAsync()
        {
            try
            {
                Home = await _glossaryService.GetGlossaryHomeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de inicio del glosario");
            }
        }
    }
}
