using eiibd26.Configuration;
using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Services.AI;
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
        private readonly ISimilarQuestionDetector _similarQuestionDetector;
        private readonly AiAnswerConfiguration _config;
        private readonly ILogger<AiAnswerJob> _logger;

        public AiAnswerJob(
            ApplicationDbContext db,
            IAiAnswerService aiAnswerService,
            IAiSafetyService aiSafetyService,
            ISimilarQuestionDetector similarQuestionDetector,
            IOptions<AiAnswerConfiguration> config,
            ILogger<AiAnswerJob> logger)
        {
            _db = db;
            _aiAnswerService = aiAnswerService;
            _aiSafetyService = aiSafetyService;
            _similarQuestionDetector = similarQuestionDetector;
            _config = config.Value;
            _logger = logger;
        }

        /// <summary>
        /// Procesa una pregunta y genera respuesta de IA si es necesario
        /// </summary>
        /// <param name="preguntaId">ID de la pregunta</param>
        /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas</param>
        public Task ProcesarPreguntaAsync(Guid preguntaId) => ProcesarPreguntaAsync(preguntaId, CancellationToken.None);

        public async Task ProcesarPreguntaAsync(Guid preguntaId, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            _logger.LogInformation(
                "[AI Job] Started for PreguntaId={PreguntaId}",
                preguntaId);

            var log = new AIRequestLog
            {
                Id = Guid.NewGuid(),
                PreguntaId = preguntaId,
                Timestamp = startTime,
                Level = Models.AI.QuestionLevel.Simple,
                HighRisk = false,
                Success = false
            };

            try
            {
                // Check cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Verificar si el servicio está habilitado
                if (!_config.Enabled)
                {
                    _logger.LogWarning("[AI Job] Service disabled, skipping pregunta {PreguntaId}", preguntaId);
                    return;
                }

                // 2. Cargar la pregunta CON sus relaciones para contexto
                var pregunta = await _db.Preguntas
                    .Include(p => p.Respuestas)
                    .Include(p => p.PreguntaCondiciones).ThenInclude(pc => pc.Condicion)
                    .Include(p => p.PreguntaSintomas).ThenInclude(ps => ps.Sintoma)
                    .Include(p => p.PreguntaTratamientos).ThenInclude(pt => pt.Tratamiento)
                    .FirstOrDefaultAsync(p => p.Id == preguntaId && !p.Eliminado);

                if (pregunta == null)
                {
                    _logger.LogWarning("[AI Job] Pregunta {PreguntaId} not found or deleted", preguntaId);
                    return;
                }

                _logger.LogInformation(
                    "[AI Job] Pregunta loaded: {PreguntaId}, Condiciones={CondCount}, Sintomas={SintCount}, Tratamientos={TratCount}",
                    preguntaId,
                    pregunta.PreguntaCondiciones?.Count ?? 0,
                    pregunta.PreguntaSintomas?.Count ?? 0,
                    pregunta.PreguntaTratamientos?.Count ?? 0);

                // 3. Verificar si ya tiene respuesta de IA
                if (pregunta.TieneRespuestaIA)
                {
                    _logger.LogInformation("[AI Job] Pregunta {PreguntaId} already has AI response, skipping", preguntaId);
                    return;
                }

                // 4. Verificar si ya tiene respuestas humanas
                var respuestasHumanas = pregunta.Respuestas.Count(r => !r.Eliminado && !r.EsIA);
                if (respuestasHumanas > 0)
                {
                    _logger.LogInformation(
                        "[AI Job] Pregunta {PreguntaId} has {Count} human responses, skipping AI generation",
                        preguntaId, respuestasHumanas);
                    return;
                }

                // 5. Buscar preguntas similares con respuesta IA existente
                var respuestaSimilar = await _similarQuestionDetector.BuscarRespuestaSimilarAsync(
                    pregunta, 
                    umbralSimilitud: 0.80, // 80% de similitud requerida
                    cancellationToken);

                string contenidoFinal;
                bool esReutilizada = false;

                if (respuestaSimilar != null)
                {
                    _logger.LogInformation(
                        "[AI Job] Reusing response from similar question (RespuestaId={RespuestaId})",
                        respuestaSimilar.Id);

                    contenidoFinal = respuestaSimilar.Cuerpo;
                    esReutilizada = true;

                    // Agregar nota al inicio de la respuesta reutilizada
                    var notaReutilizacion = "<div style='background-color: #e8f5e9; border-left: 4px solid #4caf50; padding: 12px; margin-bottom: 16px;'>" +
                                            "<strong>💡 Nota:</strong> Esta respuesta ha sido generada previamente por NINA para una pregunta similar y fue validada como útil." +
                                            "</div>";
                    contenidoFinal = notaReutilizacion + contenidoFinal;
                }
                else
                {
                    _logger.LogInformation("[AI Job] No similar question found, generating new response for {PreguntaId}", preguntaId);

                    // 6. Generar respuesta de IA CON CONTEXTO de relaciones
                    cancellationToken.ThrowIfCancellationRequested();

                    var generationStart = DateTimeOffset.UtcNow;

                    // Construir contexto dinámico con las relaciones de la pregunta
                    var contextoDinamico = BuildContextoFromRelaciones(pregunta);

                    var contenidoGenerado = await _aiAnswerService.GenerarRespuestaAsync(pregunta, cancellationToken, contextoDinamico);

                    var generationTime = (DateTimeOffset.UtcNow - generationStart).TotalSeconds;

                    _logger.LogInformation(
                        "[AI Job] Response generated in {Duration:F2}s",
                        generationTime);

                    // 7. Validar seguridad del contenido
                    bool safetyPassed = _aiSafetyService.ValidarContenido(contenidoGenerado);

                    if (safetyPassed)
                    {
                        contenidoFinal = _aiSafetyService.AgregarDisclaimer(contenidoGenerado);
                    }
                    else
                    {
                        contenidoFinal = _aiSafetyService.ObtenerRespuestaFallback();
                        _logger.LogWarning("[AI Job] Safety validation failed for pregunta {PreguntaId}, using fallback", preguntaId);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // 8. Crear la respuesta en la base de datos

                string cuerpoHtml;
                if (esReutilizada)
                {
                    // La respuesta ya viene en HTML, solo usarla
                    cuerpoHtml = contenidoFinal;
                }
                else
                {
                    // Convertir Markdown a HTML con pipeline mejorado para formato consistente
                    var pipeline = new Markdig.MarkdownPipelineBuilder()
                        .UseAdvancedExtensions()
                        .Build();

                    cuerpoHtml = Markdown.ToHtml(contenidoFinal, pipeline);

                    // Normalizar el HTML para consistencia visual
                    cuerpoHtml = NormalizarHtmlRespuesta(cuerpoHtml);

                }

                var modeloUsado = esReutilizada ? "NINA-Reused" : _config.Model;

                var respuestaIA = new Respuesta
                {
                    Id = Guid.NewGuid(),
                    PreguntaId = preguntaId,
                    UsuarioId = _config.SystemUserId,
                    Cuerpo = cuerpoHtml,
                    EsAceptada = false,
                    EsIA = true,
                    ModeloIA = modeloUsado,
                    EsColapsada = false, // Mostrar expandida por defecto cuando es la única respuesta
                    Puntuacion = 0,
                    Eliminado = false,
                    FechaCreacion = DateTimeOffset.UtcNow,
                    FechaModificacion = null,
                    ParentRespuestaId = null
                };

                // 8. Guardar Respuesta IA + marcas de pregunta (crítico — SaveChanges propio)
                // El log de auditoría va en un segundo SaveChanges separado para que un
                // fallo del log NUNCA revierta la respuesta IA que el usuario ya necesita.
                _db.Respuestas.Add(respuestaIA);
                pregunta.TieneRespuestaIA = true;
                pregunta.FechaGeneracionIA = DateTimeOffset.UtcNow;

                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation(
                        "[AI Job] Respuesta guardada: PreguntaId={PreguntaId}, RespuestaId={RespuestaId}, Reused={Reused}",
                        preguntaId, respuestaIA.Id, esReutilizada);
                }
                catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message?.Contains("UX_Respuestas_OneAIAnswerPerQuestion") == true)
                {
                    _logger.LogWarning(
                        "[AI Job] Duplicate response blocked by DB constraint: PreguntaId={PreguntaId}",
                        preguntaId);
                    return;
                }

                // 9. Log de auditoría — fallo aquí NO revierte la respuesta ya persistida
                try
                {
                    log.Success = true;
                    log.ModelUsed = modeloUsado;
                    log.QuestionText = pregunta.Titulo ?? string.Empty;
                    log.ProcessingTimeMs = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    _db.AIRequestLogs.Add(log);
                    await _db.SaveChangesAsync(CancellationToken.None);

                    _logger.LogInformation(
                        "[AI Job] Completed: PreguntaId={PreguntaId}, Duration={TotalTime:F2}s",
                        preguntaId, log.ProcessingTimeMs / 1000.0);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx,
                        "[AI Job] No se pudo guardar el audit log para PreguntaId={PreguntaId} (respuesta IA ya persistida)",
                        preguntaId);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Handles both TaskCanceledException (timeout) and OperationCanceledException (user cancellation)
                var isTimeout = ex is TaskCanceledException;
                if (isTimeout)
                {
                    _logger.LogError(
                        "[AI Job] Timeout: PreguntaId={PreguntaId}",
                        preguntaId);
                }
                else
                {
                    _logger.LogWarning(
                        "[AI Job] Cancelled: PreguntaId={PreguntaId}",
                        preguntaId);
                }
                throw; // Let Hangfire handle cancellation/retry
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "[AI Job] Network error: PreguntaId={PreguntaId}",
                    preguntaId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AI Job] Failed: PreguntaId={PreguntaId}",
                    preguntaId);

                try
                {
                    log.Success = false;
                    log.ErrorMessage = ex.Message;
                    log.ProcessingTimeMs = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    _db.AIRequestLogs.Add(log);
                    await _db.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "[AI Job] Could not save error log for PreguntaId={PreguntaId}", preguntaId);
                }

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
