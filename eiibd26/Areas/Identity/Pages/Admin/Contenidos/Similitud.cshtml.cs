using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using eiibd26.Services.Cobertura;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    /// <summary>
    /// Motor de Cobertura — Fase 3. Panel admin para disparar el cálculo de similitud
    /// (job Hangfire), ver el progreso (polling) e inspeccionar los pares de mayor score.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class SimilitudModel : PageModel
    {
        private readonly ISimilitudService _svc;
        private readonly IBackgroundJobClient _jobs;
        private readonly ILogger<SimilitudModel> _logger;

        public SimilitudModel(ISimilitudService svc, IBackgroundJobClient jobs, ILogger<SimilitudModel> logger)
        {
            _svc = svc;
            _jobs = jobs;
            _logger = logger;
        }

        public bool TablaDisponible { get; private set; } = true;
        public string? Aviso { get; private set; }

        public long TotalEstimado { get; private set; }
        public int Guardados { get; private set; }
        public DateTime? Ultimo { get; private set; }
        public List<TopParDto> TopPares { get; private set; } = new();

        #region LEGACY — FIRMA POR CONTEO (standby 09JUL)
        // Similitud por conteo (firma) jubilada a favor de embeddings Voyage. El snapshot de
        // CoberturaSimilitud se conserva (Cobertura lo lee vía ?motor=firma); ya NO se recalcula.
        // Reactivar temporalmente: FirmaLegacyActiva => true.
        public bool FirmaLegacyActiva => false;
        #endregion

        public async Task OnGetAsync()
        {
            try
            {
                (TotalEstimado, Guardados, Ultimo) = await _svc.ObtenerProgresoAsync();
                TopPares = await _svc.ObtenerTopParesAsync(50);
            }
            catch (Exception ex)
            {
                TablaDisponible = false;
                Aviso = "No se pudo leer CoberturaSimilitud. ¿Ejecutaste SQL/create-coberturasimilitud.sql? Detalle: " + ex.Message;
                _logger.LogWarning(ex, "[Similitud] No se pudo leer el estado de similitud.");
            }
        }

        /// <summary>Poll JSON del progreso en memoria de la corrida actual.</summary>
        public IActionResult OnGetProgreso()
        {
            var total = SimilitudProgreso.TotalEstimado;
            var proc = SimilitudProgreso.Procesados;
            return new JsonResult(new
            {
                corriendo = SimilitudProgreso.Corriendo,
                procesados = proc,
                total,
                guardados = SimilitudProgreso.Guardados,
                pct = total > 0 ? (int)(proc * 100 / total) : 0
            });
        }

        public IActionResult OnPostCalcular()
        {
            if (!FirmaLegacyActiva)
            {
                TempData["Error"] = "⏸️ Motor de similitud por firma en standby (jubilado por embeddings). El snapshot se conserva; no se recalcula.";
                return RedirectToPage();
            }
            _jobs.Enqueue<eiibd26.Jobs.SimilitudJob>(j => j.CalcularAsync());
            _logger.LogInformation("[Similitud] Job de cálculo encolado por admin.");
            TempData["Success"] = "✅ Cálculo de similitud encolado. Corre en segundo plano; el progreso se actualiza abajo.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetAsync()
        {
            if (!FirmaLegacyActiva)
            {
                TempData["Error"] = "⏸️ Motor de similitud por firma en standby. El snapshot se conserva; no se recalcula.";
                return RedirectToPage();
            }
            var borrados = await _svc.ResetearAsync();
            _jobs.Enqueue<eiibd26.Jobs.SimilitudJob>(j => j.CalcularAsync());
            _logger.LogInformation("[Similitud] Recálculo total: {Count} pares borrados y job re-encolado.", borrados);
            TempData["Success"] = $"🔄 {borrados} pares borrados. Recálculo total encolado.";
            return RedirectToPage();
        }
    }
}
