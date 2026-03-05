using eiibd26.Data;
using eiibd26.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace eiibd26.Controllers
{
    /// <summary>
    /// Controller de testing para diagnóstico del sistema de IA
    /// SOLO PARA DESARROLLO - ELIMINAR EN PRODUCCIÓN
    /// </summary>
    [ApiController]
    [Route("api/test/ai")]
    public class AiTestController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AiTestController> _logger;

        public AiTestController(
            ApplicationDbContext db,
            IServiceProvider serviceProvider,
            ILogger<AiTestController> logger)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/test/ai/status
        /// Verifica el estado del sistema de IA
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var config = HttpContext.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<eiibd26.Configuration.AiAnswerConfiguration>>()
                    .Value;

                // Verificar que el usuario con el GUID configurado existe
                var systemUserId = config.SystemUserId;
                var systemUserExists = await _db.Users
                    .AnyAsync(u => u.Id == systemUserId);

                // Obtener info del usuario sistema
                var systemUser = await _db.Users
                    .Where(u => u.Id == systemUserId)
                    .Select(u => new { u.UserName, u.Email })
                    .FirstOrDefaultAsync();

                var totalPreguntas = await _db.Preguntas
                    .CountAsync(p => !p.Eliminado);

                var preguntasConIA = await _db.Preguntas
                    .CountAsync(p => !p.Eliminado && p.TieneRespuestaIA);

                var respuestasIA = await _db.Respuestas
                    .CountAsync(r => !r.Eliminado && r.EsIA);

                var ultimaPregunta = await _db.Preguntas
                    .Where(p => !p.Eliminado)
                    .OrderByDescending(p => p.FechaCreacion)
                    .Select(p => new
                    {
                        p.Id,
                        p.Titulo,
                        p.FechaCreacion,
                        p.TieneRespuestaIA,
                        TotalRespuestas = _db.Respuestas.Count(r => r.PreguntaId == p.Id && !r.Eliminado)
                    })
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    timestamp = DateTimeOffset.UtcNow,
                    status = "ok",
                    configuration = new
                    {
                        enabled = config.Enabled,
                        hasApiKey = !string.IsNullOrWhiteSpace(config.AnthropicApiKey) 
                            && config.AnthropicApiKey != "ANTHROPIC_API_KEY_AQUI",
                        model = config.Model,
                        hasSystemUserId = config.SystemUserId != Guid.Empty,
                        systemUserId = config.SystemUserId
                    },
                    database = new
                    {
                        systemUserExists,
                        systemUserName = systemUser?.UserName,
                        systemUserEmail = systemUser?.Email,
                        totalPreguntas,
                        preguntasConIA,
                        respuestasIA,
                        ultimaPregunta
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo status de IA");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// POST /api/test/ai/force/{preguntaId}
        /// Fuerza la generación de respuesta IA para una pregunta específica
        /// TEMPORAL: Sin autorización para debugging
        /// </summary>
        [HttpPost("force/{preguntaId:guid}")]
        // [Authorize(Roles = "Administrador")] // TODO: Descomentar en producción
        public async Task<IActionResult> ForceGeneration(Guid preguntaId)
        {
            try
            {
                _logger.LogInformation("⚡ FORZANDO generación de IA para pregunta {PreguntaId}", preguntaId);

                var pregunta = await _db.Preguntas
                    .Include(p => p.Respuestas)
                    .FirstOrDefaultAsync(p => p.Id == preguntaId && !p.Eliminado);

                if (pregunta == null)
                {
                    return NotFound(new { error = "Pregunta no encontrada" });
                }

                _logger.LogInformation("✅ Pregunta encontrada: {Titulo}", pregunta.Titulo);
                _logger.LogInformation("   - TieneRespuestaIA: {TieneRespuestaIA}", pregunta.TieneRespuestaIA);
                _logger.LogInformation("   - Total respuestas: {Count}", pregunta.Respuestas.Count);

                // Ejecutar el job directamente en el request (para ver errores inmediatos)
                using var scope = _serviceProvider.CreateScope();
                var aiJob = scope.ServiceProvider.GetRequiredService<AiAnswerJob>();

                _logger.LogInformation("🚀 Iniciando AiAnswerJob.ProcesarPreguntaAsync...");
                await aiJob.ProcesarPreguntaAsync(preguntaId);
                _logger.LogInformation("✅ Job completado");

                // Recargar pregunta para ver el resultado
                await _db.Entry(pregunta).ReloadAsync();

                var respuestaIA = await _db.Respuestas
                    .FirstOrDefaultAsync(r => r.PreguntaId == preguntaId && !r.Eliminado && r.EsIA);

                return Ok(new
                {
                    success = true,
                    preguntaId,
                    tieneRespuestaIA = pregunta.TieneRespuestaIA,
                    fechaGeneracionIA = pregunta.FechaGeneracionIA,
                    respuestaGenerada = respuestaIA != null,
                    respuestaId = respuestaIA?.Id,
                    longitudRespuesta = respuestaIA?.Cuerpo?.Length,
                    message = "Generación forzada completada. Revisa los logs para más detalles."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR al forzar generación de IA para pregunta {PreguntaId}", preguntaId);
                return StatusCode(500, new
                {
                    error = ex.Message,
                    type = ex.GetType().Name,
                    stackTrace = ex.StackTrace,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// POST /api/test/ai/force-latest
        /// Fuerza la generación para la última pregunta creada
        /// TEMPORAL: Sin autorización para debugging
        /// </summary>
        [HttpPost("force-latest")]
        // [Authorize(Roles = "Administrador")] // TODO: Descomentar en producción
        public async Task<IActionResult> ForceLatest()
        {
            var ultimaPregunta = await _db.Preguntas
                .Where(p => !p.Eliminado)
                .OrderByDescending(p => p.FechaCreacion)
                .FirstOrDefaultAsync();

            if (ultimaPregunta == null)
                return NotFound(new { error = "No hay preguntas en la base de datos" });

            return await ForceGeneration(ultimaPregunta.Id);
        }

        /// <summary>
        /// GET /api/test/ai/test-connection
        /// Prueba la conexión directa con la API de Claude
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                _logger.LogInformation("🔍 Probando conexión directa con API de Claude...");

                var config = HttpContext.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<eiibd26.Configuration.AiAnswerConfiguration>>()
                    .Value;

                using var httpClient = new HttpClient();
                // Asegurar que BaseAddress termina con /
                var baseUrl = config.ApiBaseUrl.TrimEnd('/') + "/";
                httpClient.BaseAddress = new Uri(baseUrl);

                // Log API Key (primeros y últimos 10 caracteres para verificar)
                var apiKeyPreview = config.AnthropicApiKey.Length > 20 
                    ? $"{config.AnthropicApiKey.Substring(0, 10)}...{config.AnthropicApiKey.Substring(config.AnthropicApiKey.Length - 10)}"
                    : "KEY_TOO_SHORT";
                _logger.LogInformation("API Key length: {Length}, Preview: {Preview}", 
                    config.AnthropicApiKey.Length, apiKeyPreview);

                httpClient.DefaultRequestHeaders.Add("x-api-key", config.AnthropicApiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", config.ApiVersion);
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var requestBody = new
                {
                    model = config.Model,
                    max_tokens = 50,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = "Responde solo 'Hola' en español."
                        }
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("Enviando request a: {Url}", $"{httpClient.BaseAddress}messages");
                _logger.LogInformation("Headers: x-api-key={HasKey}, anthropic-version={Version}", 
                    !string.IsNullOrWhiteSpace(config.AnthropicApiKey), config.ApiVersion);
                _logger.LogInformation("Request body: {Body}", jsonContent);

                var response = await httpClient.PostAsync("messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Response body: {Body}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        success = false,
                        statusCode = (int)response.StatusCode,
                        statusText = response.StatusCode.ToString(),
                        error = responseContent,
                        requestUrl = $"{httpClient.BaseAddress}messages",
                        model = config.Model
                    });
                }

                return Ok(new
                {
                    success = true,
                    statusCode = (int)response.StatusCode,
                    response = responseContent,
                    message = "Conexión exitosa con API de Claude"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al probar conexión con Claude API");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    type = ex.GetType().Name,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
