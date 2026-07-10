using System;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Models.Contenidos;
using eiibd26.Services.Contenidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    /// <summary>
    /// Vista "Oportunidades de contenido" (F1) — para CM/Editor.
    /// Convierte la data del Motor de Cobertura en un backlog accionable, con vocabulario de
    /// editor (sin scores). Reutiliza el cálculo existente vía <see cref="IOportunidadesService"/>;
    /// no recalcula ni toca el panel técnico de Cobertura/Similitud.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class OportunidadesModel : PageModel
    {
        private readonly IOportunidadesService _svc;
        private readonly ILogger<OportunidadesModel> _logger;

        public OportunidadesModel(IOportunidadesService svc, ILogger<OportunidadesModel> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? Fuente { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool VerDescartados { get; set; }

        public OportunidadesVistaDto Vista { get; private set; } = new();

        public async Task OnGetAsync()
        {
            Vista = await _svc.ObtenerAsync(Fuente, VerDescartados);
        }

        /// <summary>Persiste el estado del backlog de una oportunidad. Devuelve JSON { ok, estado }.</summary>
        public async Task<IActionResult> OnPostEstadoAsync(
            [FromForm] string? tipo,
            [FromForm] int refId,
            [FromForm] byte estado)
        {
            var tipoNorm = string.Equals(tipo, OportunidadEstado.TipoPropio, StringComparison.OrdinalIgnoreCase)
                ? OportunidadEstado.TipoPropio
                : OportunidadEstado.TipoExterno;

            if (!Enum.IsDefined(typeof(EstadoBacklog), estado))
                return new JsonResult(new { ok = false, error = "Estado inválido." });

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _svc.ActualizarEstadoAsync(tipoNorm, refId, (EstadoBacklog)estado, userId);
                return new JsonResult(new { ok = true, estado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Oportunidades] Error al guardar estado tipo={Tipo} refId={RefId}", tipoNorm, refId);
                return new JsonResult(new { ok = false, error = ex.Message });
            }
        }
    }
}
