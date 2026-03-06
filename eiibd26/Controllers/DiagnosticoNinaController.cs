using eiibd26.Services.AI;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace eiibd26.Controllers
{
    /// <summary>
    /// Controlador de diagnóstico simple para verificar NINA Router
    /// </summary>
    [ApiController]
    [Route("api/diagnostico-nina")]
    [Authorize(Roles = "Administrador")]
    public class DiagnosticoNinaController : ControllerBase
    {
        private readonly IAIModelRouter _ninaRouter;
        private readonly ILogger<DiagnosticoNinaController> _logger;

        public DiagnosticoNinaController(
            IAIModelRouter ninaRouter,
            ILogger<DiagnosticoNinaController> logger)
        {
            _ninaRouter = ninaRouter;
            _logger = logger;
        }

        /// <summary>
        /// Test simple del NINA Router sin tocar la base de datos
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestSimple([FromQuery] string pregunta = "¿Qué es la colitis ulcerosa?")
        {
            _logger.LogInformation("🧪 [DIAGNOSTICO] Iniciando test simple con pregunta: '{Pregunta}'", pregunta);

            try
            {
                // Crear pregunta ficticia mínima
                var preguntaTest = new Pregunta
                {
                    Id = Guid.NewGuid(),
                    Titulo = pregunta,
                    Cuerpo = pregunta,
                    UsuarioId = Guid.Empty,
                    FechaCreacion = DateTimeOffset.UtcNow
                };

                _logger.LogInformation("🧪 [DIAGNOSTICO] Llamando a _ninaRouter.AskAsync()...");
                
                var aiResponse = await _ninaRouter.AskAsync(preguntaTest, "", CancellationToken.None);

                _logger.LogInformation(
                    "✅ [DIAGNOSTICO] Respuesta recibida: Model={Model}, Level={Level}, Risk={Risk}, Length={Length}",
                    aiResponse.Source, aiResponse.Level, aiResponse.HighRisk, aiResponse.Content?.Length ?? 0);

                return Ok(new
                {
                    ok = true,
                    pregunta,
                    respuesta = new
                    {
                        contenido = aiResponse.Content,
                        modelo = aiResponse.Source,
                        nivel = aiResponse.Level.ToString(),
                        altoRiesgo = aiResponse.HighRisk,
                        tiempoSegundos = aiResponse.ProcessingTimeSeconds
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DIAGNOSTICO] Error en test: {Message}", ex.Message);
                
                return StatusCode(500, new
                {
                    ok = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Verifica solo la detección de riesgo (más rápido)
        /// </summary>
        [HttpGet("test-risk")]
        public IActionResult TestRiskDetection([FromQuery] string texto = "tengo fiebre alta y vomito sangre")
        {
            _logger.LogInformation("🧪 [DIAGNOSTICO] Test de detección de riesgo: '{Texto}'", texto);

            try
            {
                var isHighRisk = _ninaRouter.DetectHighRisk(texto);

                return Ok(new
                {
                    ok = true,
                    texto,
                    altoRiesgo = isHighRisk
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DIAGNOSTICO] Error en test de riesgo: {Message}", ex.Message);
                
                return StatusCode(500, new
                {
                    ok = false,
                    error = ex.Message
                });
            }
        }
    }
}
