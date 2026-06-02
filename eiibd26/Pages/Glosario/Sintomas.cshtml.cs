using eiibd26.Models.Glossary;
using eiibd26.Models.Validacion;
using eiibd26.Services.Glossary;
using eiibd26.Services.Glossary.DTOs;
using eiibd26.Services.Validacion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages.Glosario
{
    public class SintomasModel : PageModel
    {
        private readonly IGlossaryService _glossaryService;
        private readonly IValidacionContenidoService _validacionSvc;
        private readonly ILogger<SintomasModel> _logger;

        public SintomasModel(
            IGlossaryService glossaryService,
            IValidacionContenidoService validacionSvc,
            ILogger<SintomasModel> logger)
        {
            _glossaryService = glossaryService;
            _validacionSvc = validacionSvc;
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
                Terminos = await _glossaryService.GetTermsByTypeAsync(GlossaryTermType.Sintoma);
                _logger.LogInformation("Página de síntomas cargada con {Count} términos", Terminos.Count);

                var ids = Terminos.Select(t => t.Id).ToList();
                var validadoresDict = await _validacionSvc.ObtenerValidadoresGlosarioPorTerminosAsync(ids);
                foreach (var t in Terminos)
                    if (validadoresDict.TryGetValue(t.Id, out var v))
                        t.Validadores = v;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de síntomas");
            }
        }
    }
}
