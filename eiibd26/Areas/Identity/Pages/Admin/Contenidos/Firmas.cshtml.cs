using System;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Services.Cobertura;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    /// <summary>
    /// Motor de Cobertura — Fase 1. Panel admin para disparar y monitorear el
    /// cálculo de firmas de los contenidos publicados. Solo lectura de estado +
    /// dos acciones (recalcular pendientes / forzar recálculo total).
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class FirmasModel : PageModel
    {
        private readonly IFirmaService _firmaService;
        private readonly ApplicationDbContext _db;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly ILogger<FirmasModel> _logger;

        public FirmasModel(
            IFirmaService firmaService,
            ApplicationDbContext db,
            IBackgroundJobClient backgroundJobs,
            ILogger<FirmasModel> logger)
        {
            _firmaService = firmaService;
            _db = db;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public int Total { get; private set; }
        public int Firmados { get; private set; }
        public int Pendientes => Total - Firmados;
        public DateTime? UltimaFirma { get; private set; }

        public async Task OnGetAsync()
        {
            (Total, Firmados) = await _firmaService.ObtenerProgresoAsync();
            UltimaFirma = await _db.Contenidos
                .Where(c => c.FirmaCalculadaEn != null)
                .MaxAsync(c => (DateTime?)c.FirmaCalculadaEn);
        }

        /// <summary>Poll JSON para la barra de progreso: { total, firmados, pendientes }.</summary>
        public async Task<IActionResult> OnGetProgresoAsync()
        {
            var (total, firmados) = await _firmaService.ObtenerProgresoAsync();
            return new JsonResult(new { total, firmados, pendientes = total - firmados });
        }

        /// <summary>Encola el job que firma solo los pendientes (Firma IS NULL).</summary>
        public IActionResult OnPostRecalcular()
        {
            _backgroundJobs.Enqueue<eiibd26.Jobs.FirmaContenidoJob>(j => j.FirmarPendientesAsync());
            _logger.LogInformation("[Firmas] Job de firma (pendientes) encolado por admin.");
            TempData["Success"] = "✅ Cálculo de firmas encolado. Se firmarán los pendientes en segundo plano.";
            return RedirectToPage();
        }

        /// <summary>Resetea todas las firmas a NULL y luego encola el job (recálculo desde cero).</summary>
        public async Task<IActionResult> OnPostForzarAsync()
        {
            var reseteadas = await _firmaService.ResetearFirmasAsync();
            _backgroundJobs.Enqueue<eiibd26.Jobs.FirmaContenidoJob>(j => j.FirmarPendientesAsync());
            _logger.LogInformation("[Firmas] Recálculo total forzado por admin. {Count} firmas reseteadas.", reseteadas);
            TempData["Success"] = $"🔄 {reseteadas} firmas reseteadas. Recálculo total encolado en segundo plano.";
            return RedirectToPage();
        }
    }
}
