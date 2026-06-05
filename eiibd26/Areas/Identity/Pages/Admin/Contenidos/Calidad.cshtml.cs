using eiibd26.Services.Calidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    [Authorize(Roles = "Administrador")]
    public class CalidadModel : PageModel
    {
        private readonly IContenidoCalidadService _calidad;
        private readonly IGrisEvaluadorService _gris;
        private readonly ILogger<CalidadModel> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public CalidadModel(
            IContenidoCalidadService calidad,
            IGrisEvaluadorService gris,
            ILogger<CalidadModel> logger)
        {
            _calidad = calidad;
            _gris = gris;
            _logger = logger;
        }

        public List<ContenidoCalidadDto>? ResultadosGuardados { get; private set; }
        public DateTime? UltimoAnalisis { get; private set; }

        public async Task OnGetAsync()
        {
            var guardados = await _calidad.ObtenerResultadosGuardadosAsync();
            if (guardados != null)
            {
                ResultadosGuardados = guardados.Resultados;
                UltimoAnalisis = guardados.UltimoAnalisis;
            }
        }

        /// <summary>
        /// Análisis mecánico por batch. Devuelve JSON:
        /// { total: N, items: [ ContenidoCalidadDto... ] }
        /// </summary>
        public async Task<IActionResult> OnPostAnalizarBatchAsync(
            [FromForm] int skip,
            [FromForm] int take = 10)
        {
            if (take <= 0 || take > 50) take = 10;
            if (skip < 0) skip = 0;

            var resultado = await _calidad.AnalizarBatchAsync(skip, take);
            return new JsonResult(resultado, JsonOpts);
        }

        /// <summary>
        /// Evaluación editorial GRIS con IA para un contenido individual.
        /// Devuelve JSON: GrisEvaluacionDto.
        /// </summary>
        public async Task<IActionResult> OnPostEvaluarGrisAsync([FromForm] int id)
        {
            try
            {
                var resultado = await _gris.EvaluarAsync(id);
                return new JsonResult(resultado, JsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GRIS] Error al evaluar contenido {Id} desde Calidad", id);
                return new JsonResult(new { ok = false, error = ex.Message }, JsonOpts);
            }
        }
    }
}
