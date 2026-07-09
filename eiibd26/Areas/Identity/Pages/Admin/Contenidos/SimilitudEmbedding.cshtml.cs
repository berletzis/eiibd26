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
    /// Motor de Cobertura — Fase 5. Panel admin para disparar el cálculo de similitud por
    /// EMBEDDINGS (coseno denso), ver el progreso (polling) e inspeccionar los pares de mayor
    /// score. Clon del panel de Similitud (firma), escribe en CoberturaSimilitudEmbedding.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class SimilitudEmbeddingModel : PageModel
    {
        private readonly ISimilitudEmbeddingService _svc;
        private readonly IBackgroundJobClient _jobs;
        private readonly ILogger<SimilitudEmbeddingModel> _logger;

        public SimilitudEmbeddingModel(ISimilitudEmbeddingService svc, IBackgroundJobClient jobs, ILogger<SimilitudEmbeddingModel> logger)
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
                Aviso = "No se pudo leer CoberturaSimilitudEmbedding. ¿Ejecutaste SQL/embeddings-fase2.sql? Detalle: " + ex.Message;
                _logger.LogWarning(ex, "[SimEmbedding] No se pudo leer el estado de similitud por embeddings.");
            }
        }

        /// <summary>Poll JSON del progreso en memoria de la corrida actual.</summary>
        public IActionResult OnGetProgreso()
        {
            var total = SimilitudEmbeddingProgreso.TotalEstimado;
            var proc = SimilitudEmbeddingProgreso.Procesados;
            return new JsonResult(new
            {
                corriendo = SimilitudEmbeddingProgreso.Corriendo,
                procesados = proc,
                total,
                guardados = SimilitudEmbeddingProgreso.Guardados,
                pct = total > 0 ? (int)(proc * 100 / total) : 0
            });
        }

        public IActionResult OnPostCalcular()
        {
            _jobs.Enqueue<eiibd26.Jobs.SimilitudEmbeddingJob>(j => j.CalcularAsync());
            _logger.LogInformation("[SimEmbedding] Job de cálculo encolado por admin.");
            TempData["Success"] = "✅ Cálculo de similitud por embeddings encolado. Corre en segundo plano; el progreso se actualiza abajo.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetAsync()
        {
            var borrados = await _svc.ResetearAsync();
            _jobs.Enqueue<eiibd26.Jobs.SimilitudEmbeddingJob>(j => j.CalcularAsync());
            _logger.LogInformation("[SimEmbedding] Recálculo total: {Count} pares borrados y job re-encolado.", borrados);
            TempData["Success"] = $"🔄 {borrados} pares borrados. Recálculo total encolado.";
            return RedirectToPage();
        }
    }
}
