using eiibd26.Configuration;
using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Services.AI;
// using Hangfire; // TODO: Uncomment after installing Hangfire packages
using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eiibd26.Jobs
{
    /// <summary>
    /// Job en segundo plano para generar respuestas de IA automáticas
    /// Se ejecuta de forma asíncrona después de crear una pregunta
    /// </summary>
    public class AiAnswerJob
    {
        private readonly ApplicationDbContext _db;
        private readonly IAiAnswerService _aiAnswerService;
        private readonly IAiSafetyService _aiSafetyService;
        private readonly AiAnswerConfiguration _config;
        private readonly ILogger<AiAnswerJob> _logger;

        public AiAnswerJob(
            ApplicationDbContext db,
            IAiAnswerService aiAnswerService,
            IAiSafetyService aiSafetyService,
            IOptions<AiAnswerConfiguration> config,
            ILogger<AiAnswerJob> logger)
        {
            _db = db;
            _aiAnswerService = aiAnswerService;
            _aiSafetyService = aiSafetyService;
            _config = config.Value;
            _logger = logger;
        }

        /// <summary>
        /// Procesa una pregunta y genera respuesta de IA si es necesario
        /// </summary>
        /// <param name="preguntaId">ID de la pregunta</param>
        /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas</param>
        // [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })] // TODO: Uncomment after Hangfire
        public async Task ProcesarPreguntaAsync(Guid preguntaId, CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            _logger.LogInformation(
                "🎯 [AI Job] INICIADO para PreguntaId={PreguntaId}, Timestamp={Timestamp}",
                preguntaId, startTime);

            try
            {
                // Check cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Verificar si el servicio está habilitado
                _logger.LogInformation("🔍 [AI Job] Verificando si servicio está habilitado...");
                if (!_config.Enabled)
                {
                    _logger.LogWarning("⚠️ [AI Job] Servicio de IA deshabilitado, saltando pregunta {PreguntaId}", preguntaId);
                    return;
                }
                _logger.LogInformation("✅ [AI Job] Servicio habilitado");

                // 2. Cargar la pregunta CON sus relaciones para contexto
                _logger.LogInformation("🔍 [AI Job] Cargando pregunta desde BD...");
                var pregunta = await _db.Preguntas
                    .Include(p => p.Respuestas)
                    .Include(p => p.PreguntaCondiciones).ThenInclude(pc => pc.Condicion)
                    .Include(p => p.PreguntaSintomas).ThenInclude(ps => ps.Sintoma)
                    .Include(p => p.PreguntaTratamientos).ThenInclude(pt => pt.Tratamiento)
                    .FirstOrDefaultAsync(p => p.Id == preguntaId && !p.Eliminado);

                if (pregunta == null)
                {
                    _logger.LogWarning("❌ [AI Job] Pregunta {PreguntaId} no encontrada o eliminada", preguntaId);
                    return;
                }

                _logger.LogInformation(
                    "✅ [AI Job] Pregunta cargada: Título='{Titulo}', Cuerpo length={CuerpoLength}, Condiciones={CondCount}, Síntomas={SintCount}, Tratamientos={TratCount}",
                    pregunta.Titulo ?? "[SIN TÍTULO]",
                    pregunta.Cuerpo?.Length ?? 0,
                    pregunta.PreguntaCondiciones?.Count ?? 0,
                    pregunta.PreguntaSintomas?.Count ?? 0,
                    pregunta.PreguntaTratamientos?.Count ?? 0);

                _logger.LogInformation(
                    "📄 [AI Job] Contenido de la pregunta:\n  Título: '{Titulo}'\n  Cuerpo: '{Cuerpo}'",
                    pregunta.Titulo ?? "[VACÍO]",
                    string.IsNullOrWhiteSpace(pregunta.Cuerpo) ? "[VACÍO]" : 
                        (pregunta.Cuerpo.Length > 200 ? pregunta.Cuerpo.Substring(0, 200) + "..." : pregunta.Cuerpo));

                // 3. Verificar si ya tiene respuesta de IA
                _logger.LogInformation("🔍 [AI Job] Verificando si ya tiene respuesta IA...");
                if (pregunta.TieneRespuestaIA)
                {
                    _logger.LogInformation("⚠️ [AI Job] Pregunta {PreguntaId} ya tiene respuesta de IA, saltando", preguntaId);
                    return;
                }

                // 4. Verificar si ya tiene respuestas humanas
                var respuestasHumanas = pregunta.Respuestas.Count(r => !r.Eliminado && !r.EsIA);
                _logger.LogInformation("🔍 [AI Job] Respuestas humanas existentes: {Count}", respuestasHumanas);
                if (respuestasHumanas > 0)
                {
                    _logger.LogInformation(
                        "⚠️ [AI Job] Pregunta {PreguntaId} ya tiene {Count} respuestas humanas, no se generará respuesta de IA",
                        preguntaId, respuestasHumanas);
                    return;
                }

                // 5. Generar respuesta de IA CON CONTEXTO de relaciones
                cancellationToken.ThrowIfCancellationRequested();

                var generationStart = DateTimeOffset.UtcNow;
                _logger.LogInformation("🤖 [AI Job] Llamando a Claude API para generar respuesta...");

                // Construir contexto dinámico con las relaciones de la pregunta
                var contextoDinamico = BuildContextoFromRelaciones(pregunta);

                if (!string.IsNullOrWhiteSpace(contextoDinamico))
                {
                    _logger.LogInformation("📋 [AI Job] Contexto dinámico construido: {Contexto}", contextoDinamico);
                }

                var contenidoGenerado = await _aiAnswerService.GenerarRespuestaAsync(pregunta, cancellationToken, contextoDinamico);

                var generationTime = (DateTimeOffset.UtcNow - generationStart).TotalSeconds;
                var estimatedTokens = contenidoGenerado.Length / 4; // Rough estimate: 1 token ≈ 4 chars

                _logger.LogInformation(
                    "✅ [AI Job] Respuesta generada en {Duration:F2}s, ~{Tokens} tokens",
                    generationTime, estimatedTokens);

                // 6. Validar seguridad del contenido
                _logger.LogInformation("🛡️ [AI Job] Validando seguridad del contenido...");
                string contenidoFinal;
                bool safetyPassed = _aiSafetyService.ValidarContenido(contenidoGenerado);

                if (safetyPassed)
                {
                    contenidoFinal = _aiSafetyService.AgregarDisclaimer(contenidoGenerado);
                    _logger.LogInformation("✅ [AI Job] Validación de seguridad APROBADA");
                }
                else
                {
                    contenidoFinal = _aiSafetyService.ObtenerRespuestaFallback();
                    _logger.LogWarning("⚠️ [AI Job] Validación de seguridad RECHAZADA, usando fallback");
                }

                cancellationToken.ThrowIfCancellationRequested();

                // 7. Crear la respuesta en la base de datos
                _logger.LogInformation("💾 [AI Job] Guardando respuesta en BD...");

                // Convertir Markdown a HTML con pipeline mejorado para formato consistente
                var pipeline = new Markdig.MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();

                var cuerpoHtml = Markdown.ToHtml(contenidoFinal, pipeline);

                // Normalizar el HTML para consistencia visual
                cuerpoHtml = NormalizarHtmlRespuesta(cuerpoHtml);

                _logger.LogInformation("✅ [AI Job] HTML normalizado generado ({Length} chars)", cuerpoHtml.Length);

                var respuestaIA = new Respuesta
                {
                    Id = Guid.NewGuid(),
                    PreguntaId = preguntaId,
                    UsuarioId = _config.SystemUserId,
                    Cuerpo = cuerpoHtml,
                    EsAceptada = false,
                    EsIA = true,
                    ModeloIA = _config.Model,
                    EsColapsada = false, // Mostrar expandida por defecto cuando es la única respuesta
                    Puntuacion = 0,
                    Eliminado = false,
                    FechaCreacion = DateTimeOffset.UtcNow,
                    FechaModificacion = null,
                    ParentRespuestaId = null
                };

                _db.Respuestas.Add(respuestaIA);

                // 8. Marcar la pregunta como que tiene respuesta de IA
                pregunta.TieneRespuestaIA = true;
                pregunta.FechaGeneracionIA = DateTimeOffset.UtcNow;

                try
                {
                    await _db.SaveChangesAsync(cancellationToken);

                    var totalTime = (DateTimeOffset.UtcNow - startTime).TotalSeconds;

                    _logger.LogInformation(
                        "🎉 [AI Job] COMPLETADO EXITOSAMENTE en {TotalTime:F2}s: PreguntaId={PreguntaId}, RespuestaId={RespuestaId}, SafetyPassed={SafetyPassed}",
                        totalTime, preguntaId, respuestaIA.Id, safetyPassed);
                }
                catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message?.Contains("UX_Respuestas_OneAIAnswerPerQuestion") == true)
                {
                    // Duplicate AI answer detected by database constraint
                    _logger.LogWarning(
                        "⚠️ [AI Job] Respuesta duplicada bloqueada por BD: PreguntaId={PreguntaId}. Otro worker ya creó la respuesta.",
                        preguntaId);

                    // This is NOT an error - it's the constraint working correctly
                    // Don't retry, just exit gracefully
                    return;
                }
            }
            catch (OperationCanceledException ex)
            {
                // Handles both TaskCanceledException (timeout) and OperationCanceledException (user cancellation)
                var isTimeout = ex is TaskCanceledException;
                if (isTimeout)
                {
                    _logger.LogError(
                        "⏰ [AI Job] TIMEOUT: PreguntaId={PreguntaId}",
                        preguntaId);
                }
                else
                {
                    _logger.LogWarning(
                        "🛑 [AI Job] CANCELADO: PreguntaId={PreguntaId}, Razón={Reason}",
                        preguntaId, ex.Message);
                }
                throw; // Let Hangfire handle cancellation/retry
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "❌ [AI Job] ERROR DE RED: PreguntaId={PreguntaId}, Error={Error}",
                    preguntaId, ex.Message);
                throw; // Permitir retry de Hangfire
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ [AI Job] FALLÓ: PreguntaId={PreguntaId}, Error={Error}",
                    preguntaId, ex.Message);
                // No hacer throw para evitar reintentos infinitos en errores desconocidos
            }
        }

        /// <summary>
        /// Normaliza el HTML generado para tener formato consistente
        /// Convierte h1/h2/h3 a negritas, evita estilos inconsistentes
        /// </summary>
        private string NormalizarHtmlRespuesta(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            // Convertir h1, h2, h3 a párrafos con negrita (más consistente)
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<h1[^>]*>(.*?)</h1>", "<p><strong>$1</strong></p>", System.Text.RegularExpressions.RegexOptions.Singleline);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<h2[^>]*>(.*?)</h2>", "<p><strong>$1</strong></p>", System.Text.RegularExpressions.RegexOptions.Singleline);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<h3[^>]*>(.*?)</h3>", "<p><strong>$1</strong></p>", System.Text.RegularExpressions.RegexOptions.Singleline);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<h4[^>]*>(.*?)</h4>", "<p><strong>$1</strong></p>", System.Text.RegularExpressions.RegexOptions.Singleline);

            // Normalizar espacios múltiples
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ");

            // Remover espacios entre tags
            html = System.Text.RegularExpressions.Regex.Replace(html, @">\s+<", "><");

            return html.Trim();
        }

        /// <summary>
        /// Construye un contexto dinámico con las relaciones de la pregunta
        /// para proporcionar mejor información a la IA
        /// </summary>
        private string BuildContextoFromRelaciones(Pregunta pregunta)
        {
            var contextoParts = new List<string>();

            // Agregar condiciones si existen
            var condiciones = pregunta.PreguntaCondiciones?
                .Where(pc => pc.Condicion != null)
                .Select(pc => pc.Condicion!.nombre)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            if (condiciones?.Any() == true)
            {
                contextoParts.Add($"**Condiciones relacionadas:** {string.Join(", ", condiciones)}");
            }

            // Agregar síntomas si existen
            var sintomas = pregunta.PreguntaSintomas?
                .Where(ps => ps.Sintoma != null)
                .Select(ps => ps.Sintoma!.nombre)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            if (sintomas?.Any() == true)
            {
                contextoParts.Add($"**Síntomas mencionados:** {string.Join(", ", sintomas)}");
            }

            // Agregar tratamientos si existen
            var tratamientos = pregunta.PreguntaTratamientos?
                .Where(pt => pt.Tratamiento != null)
                .Select(pt => pt.Tratamiento!.nombre)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            if (tratamientos?.Any() == true)
            {
                contextoParts.Add($"**Tratamientos actuales:** {string.Join(", ", tratamientos)}");
            }

            return contextoParts.Any() 
                ? string.Join("\n", contextoParts) 
                : string.Empty;
        }
    }
}
