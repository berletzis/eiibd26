using System;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Services.Cobertura;
using eiibd26.Voyage;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    /// <summary>
    /// Motor de Cobertura — Fase 5 (embeddings). Panel admin para disparar y monitorear la
    /// generación de embeddings (Voyage) de los contenidos publicados. Solo lectura de estado +
    /// dos acciones (embeber pendientes / forzar recálculo total). Clon del panel de Firmas.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class EmbeddingsModel : PageModel
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVoyageEmbeddingClient _voyage;
        private readonly ApplicationDbContext _db;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly ILogger<EmbeddingsModel> _logger;

        public EmbeddingsModel(
            IEmbeddingService embeddingService,
            IVoyageEmbeddingClient voyage,
            ApplicationDbContext db,
            IBackgroundJobClient backgroundJobs,
            ILogger<EmbeddingsModel> logger)
        {
            _embeddingService = embeddingService;
            _voyage = voyage;
            _db = db;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public int Total { get; private set; }
        public int Embebidos { get; private set; }
        public int Pendientes => Total - Embebidos;
        public DateTime? UltimoEmbedding { get; private set; }
        public bool VoyageHabilitado { get; private set; }
        public string Modelo { get; private set; } = "";

        public async Task OnGetAsync()
        {
            (Total, Embebidos) = await _embeddingService.ObtenerProgresoAsync();
            UltimoEmbedding = await _db.Contenidos
                .Where(c => c.EmbeddingCalculadoEn != null)
                .MaxAsync(c => (DateTime?)c.EmbeddingCalculadoEn);
            VoyageHabilitado = _voyage.Habilitado;
            Modelo = _voyage.Modelo;
        }

        /// <summary>Poll JSON para la barra de progreso: { total, embebidos, pendientes }.</summary>
        public async Task<IActionResult> OnGetProgresoAsync()
        {
            var (total, embebidos) = await _embeddingService.ObtenerProgresoAsync();
            return new JsonResult(new { total, embebidos, pendientes = total - embebidos });
        }

        /// <summary>Encola el job que embebe solo los pendientes (Embedding IS NULL).</summary>
        public IActionResult OnPostRecalcular()
        {
            if (!_voyage.Habilitado)
            {
                TempData["Error"] = "⚠️ Voyage no tiene API key configurada (sección Voyage:ApiKey). No se puede embeber.";
                return RedirectToPage();
            }
            _backgroundJobs.Enqueue<eiibd26.Jobs.EmbeddingContenidoJob>(j => j.EmbedPendientesAsync());
            _logger.LogInformation("[Embeddings] Job de embedding (pendientes) encolado por admin.");
            TempData["Success"] = "✅ Generación de embeddings encolada. Se embeberán los pendientes en segundo plano.";
            return RedirectToPage();
        }

        /// <summary>Resetea todos los embeddings a NULL y luego encola el job (recálculo desde cero).</summary>
        public async Task<IActionResult> OnPostForzarAsync()
        {
            if (!_voyage.Habilitado)
            {
                TempData["Error"] = "⚠️ Voyage no tiene API key configurada (sección Voyage:ApiKey). No se puede embeber.";
                return RedirectToPage();
            }
            var reseteados = await _embeddingService.ResetearEmbeddingsAsync();
            _backgroundJobs.Enqueue<eiibd26.Jobs.EmbeddingContenidoJob>(j => j.EmbedPendientesAsync());
            _logger.LogInformation("[Embeddings] Recálculo total forzado por admin. {Count} embeddings reseteados.", reseteados);
            TempData["Success"] = $"🔄 {reseteados} embeddings reseteados. Recálculo total encolado en segundo plano.";
            return RedirectToPage();
        }
    }
}
