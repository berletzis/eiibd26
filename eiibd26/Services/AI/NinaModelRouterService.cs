using eiibd26.Configuration;
using eiibd26.Models;
using eiibd26.Models.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Implementación del router NINA que decide qué modelo de IA usar
    /// para optimizar costos sin afectar calidad
    /// </summary>
    public class NinaModelRouterService : IAIModelRouter
    {
        private readonly IAiAnswerService _claudeService;
        private readonly IAiSafetyService _safetyService;
        private readonly HttpClient _httpClient;
        private readonly AiAnswerConfiguration _config;
        private readonly ILogger<NinaModelRouterService> _logger;

        // Palabras clave para detección de alto riesgo médico
        private static readonly HashSet<string> HighRiskKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "sangre", "fiebre", "dolor fuerte", "urgencias", "urgencia", "hospital",
            "efecto secundario", "efectos secundarios", "empeoró", "empeoro", 
            "empeorando", "grave", "mortal", "muerte", "suicidio", "emergencia",
            "intoxicación", "intoxicacion", "sobredosis", "convulsión", "convulsion",
            "desmayo", "inconsciente", "no responde", "pecho", "corazón", "corazon",
            "respirar", "ahogo", "asfixia", "mareo severo", "vomito sangre",
            "vómito sangre", "tos sangre"
        };

        // Modelos disponibles
        private const string ModelClaudeSonnet = "claude-sonnet-4.5-20250514";
        private const string ModelClaudeHaiku = "claude-3-5-haiku-20241022"; // Modelo económico
        private const string ModelEconomico = "Modelo Base EIIBD"; // Respuestas pre-programadas

        public NinaModelRouterService(
            IAiAnswerService claudeService,
            IAiSafetyService safetyService,
            IHttpClientFactory httpClientFactory,
            IOptions<AiAnswerConfiguration> config,
            ILogger<NinaModelRouterService> logger)
        {
            _claudeService = claudeService;
            _safetyService = safetyService;
            _httpClient = httpClientFactory.CreateClient("AnthropicClient");
            _config = config.Value;
            _logger = logger;
        }

        public async Task<AIResponse> AskAsync(
            Pregunta pregunta, 
            string? contextoDinamico = null, 
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            var fullQuestion = $"{pregunta.Titulo} {pregunta.Cuerpo}";

            _logger.LogInformation(
                "🎯 [NINA Router] Procesando pregunta {PreguntaId}: '{Titulo}'",
                pregunta.Id, pregunta.Titulo);

            // PASO 1: Detectar riesgo médico (local, sin costo)
            bool highRisk = DetectHighRisk(fullQuestion);

            if (highRisk)
            {
                _logger.LogWarning(
                    "⚠️ [NINA Router] ALTO RIESGO detectado en pregunta {PreguntaId}. Usando Claude Sonnet obligatoriamente.",
                    pregunta.Id);

                var content = await _claudeService.GenerarRespuestaAsync(pregunta, cancellationToken, contextoDinamico);
                var safeContent = _safetyService.ValidarContenido(content) 
                    ? _safetyService.AgregarDisclaimer(content)
                    : _safetyService.ObtenerRespuestaFallback();

                return new AIResponse
                {
                    Content = EnriquecerConAutoria(safeContent, ModelClaudeSonnet),
                    Source = ModelClaudeSonnet,
                    Level = QuestionLevel.Complex,
                    HighRisk = true,
                    ProcessingTimeSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds
                };
            }

            // PASO 2: Clasificar pregunta con modelo económico
            QuestionLevel level;
            try
            {
                level = await ClassifyQuestionAsync(fullQuestion, cancellationToken);
                _logger.LogInformation(
                    "📊 [NINA Router] Nivel detectado: {Level} para pregunta {PreguntaId}",
                    level, pregunta.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "⚠️ [NINA Router] Error al clasificar, usando Claude Sonnet por seguridad");
                level = QuestionLevel.Complex;
            }

            // PASO 3: Seleccionar modelo según nivel
            string modelUsado;
            string contenidoRespuesta;

            switch (level)
            {
                case QuestionLevel.Simple:
                    _logger.LogInformation("💡 [NINA Router] Intentando respuesta local EII");
                    contenidoRespuesta = GenerarRespuestaSimple(pregunta);
                    if (string.IsNullOrEmpty(contenidoRespuesta))
                    {
                        // No hay template local para este tema; escalar a Haiku
                        _logger.LogInformation("💰 [NINA Router] Sin template local, escalando a Claude Haiku");
                        contenidoRespuesta = await GenerarRespuestaConHaiku(pregunta, contextoDinamico, cancellationToken);
                        modelUsado = ModelClaudeHaiku;
                    }
                    else
                    {
                        modelUsado = ModelEconomico;
                    }
                    break;

                case QuestionLevel.Medium:
                    _logger.LogInformation("💰 [NINA Router] Usando Claude Haiku (económico)");
                    contenidoRespuesta = await GenerarRespuestaConHaiku(pregunta, contextoDinamico, cancellationToken);
                    modelUsado = ModelClaudeHaiku;
                    break;

                case QuestionLevel.Complex:
                default:
                    _logger.LogInformation("🎓 [NINA Router] Usando Claude Sonnet (premium)");
                    contenidoRespuesta = await _claudeService.GenerarRespuestaAsync(pregunta, cancellationToken, contextoDinamico);
                    modelUsado = ModelClaudeSonnet;
                    break;
            }

            // PASO 4: Validar seguridad
            var contenidoSeguro = _safetyService.ValidarContenido(contenidoRespuesta)
                ? _safetyService.AgregarDisclaimer(contenidoRespuesta)
                : _safetyService.ObtenerRespuestaFallback();

            // PASO 5: Enriquecer con autoría NINA
            var contenidoFinal = EnriquecerConAutoria(contenidoSeguro, modelUsado);

            var processingTime = (DateTimeOffset.UtcNow - startTime).TotalSeconds;

            _logger.LogInformation(
                "✅ [NINA Router] Respuesta generada en {Time:F2}s usando {Model}",
                processingTime, modelUsado);

            return new AIResponse
            {
                Content = contenidoFinal,
                Source = modelUsado,
                Level = level,
                HighRisk = false,
                ProcessingTimeSeconds = processingTime
            };
        }

        public bool DetectHighRisk(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return false;

            var questionLower = question.ToLowerInvariant();

            foreach (var keyword in HighRiskKeywords)
            {
                if (questionLower.Contains(keyword.ToLowerInvariant()))
                {
                    _logger.LogWarning(
                        "🚨 [NINA Router] Palabra de alto riesgo detectada: '{Keyword}'",
                        keyword);
                    return true;
                }
            }

            return false;
        }

        public async Task<QuestionLevel> ClassifyQuestionAsync(
            string question, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                return QuestionLevel.Medium;

            try
            {
                var prompt = $@"Analiza la siguiente pregunta sobre Enfermedad Inflamatoria Intestinal (EII) y clasifícala según su complejidad:

SIMPLE: pregunta informativa general sobre EII, CUCI o Crohn (¿Qué es...? ¿Cómo se define...? ¿Cuáles son los tipos...?)
MEDIA: requiere explicación contextual o comparación (¿Por qué...? ¿Cómo funciona...? ¿Cuál es la diferencia...?)
COMPLEJA: incluye síntomas personales, medicamentos específicos, decisiones médicas personales, o situaciones de salud individuales

Responde SOLO con una palabra: SIMPLE, MEDIA o COMPLEJA.

Pregunta:
""{question}""";

                var requestBody = new
                {
                    model = ModelClaudeHaiku, // Usar modelo económico para clasificación
                    max_tokens = 10, // Solo necesitamos una palabra
                    temperature = 0.0, // Determinista
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("[NINA Router] Enviando pregunta para clasificación...");

                var response = await _httpClient.PostAsync("messages", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonResponse = JsonDocument.Parse(responseContent);

                var classification = jsonResponse.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString()?
                    .Trim()
                    .ToUpperInvariant() ?? "MEDIA";

                _logger.LogInformation(
                    "[NINA Router] Clasificación recibida: {Classification}",
                    classification);

                return classification switch
                {
                    "SIMPLE" => QuestionLevel.Simple,
                    "MEDIA" => QuestionLevel.Medium,
                    "COMPLEJA" => QuestionLevel.Complex,
                    _ => QuestionLevel.Medium // Default fallback
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NINA Router] Error en clasificación, usando nivel MEDIO por defecto");
                return QuestionLevel.Medium;
            }
        }

        private string GenerarRespuestaSimple(Pregunta pregunta)
        {
            var titulo = pregunta.Titulo ?? string.Empty;

            // Intentar resolver con conocimiento local EII
            var respuestaLocal = IBDKnowledgeTemplates.TryResolve(titulo);
            if (respuestaLocal is not null)
                return respuestaLocal;

            // Pregunta fuera del dominio EII — redirigir sin inventar contenido clínico
            _logger.LogWarning(
                "[NINA Router] Pregunta '{Titulo}' no resuelta por templates locales EII; derivando a modelo IA.",
                pregunta.Titulo);

            // Retornar null fuerza el escalado al modelo IA en el caller
            return string.Empty;
        }

        private async Task<string> GenerarRespuestaConHaiku(
            Pregunta pregunta,
            string? contextoDinamico,
            CancellationToken cancellationToken)
        {
            // Usar Claude Haiku (más económico que Sonnet) para preguntas de complejidad media
            try
            {
                var systemPrompt = @"Eres NINA, asistente educativa especializada en Enfermedad Inflamatoria Intestinal (EII), 
incluye Colitis Ulcerosa Crónica Idiopática (CUCI) y Enfermedad de Crohn. 
Proporciona respuestas claras, empáticas y educativas exclusivamente sobre EII y su manejo. 
Usa lenguaje accesible. 
Si la pregunta involucra decisiones médicas personales, recomienda consultar a un gastroenterólogo. 
Si la pregunta no está relacionada con EII, indica amablemente que la plataforma se especializa en EII.";

                var userPrompt = $@"Pregunta: {pregunta.Titulo}

Contexto adicional: {pregunta.Cuerpo}";

                if (!string.IsNullOrWhiteSpace(contextoDinamico))
                {
                    userPrompt += $"\n\nContexto del usuario: {contextoDinamico}";
                }

                var requestBody = new
                {
                    model = ModelClaudeHaiku,
                    max_tokens = 500,
                    temperature = 0.3,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = userPrompt }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("messages", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonResponse = JsonDocument.Parse(responseContent);

                var generatedText = jsonResponse.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                return generatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NINA Router] Error con Haiku, usando Sonnet como fallback");
                return await _claudeService.GenerarRespuestaAsync(pregunta, cancellationToken, contextoDinamico);
            }
        }

        private string EnriquecerConAutoria(string contenido, string modeloUsado)
        {
            // Agregar metadata de autoría NINA al final del contenido
            var autoriaInfo = $@"

---
**Autor:** NINA  
**Fuente:** {ObtenerNombreAmigable(modeloUsado)}";

            return contenido + autoriaInfo;
        }

        private string ObtenerNombreAmigable(string modeloTecnico)
        {
            return modeloTecnico switch
            {
                ModelClaudeSonnet => "Claude Sonnet",
                ModelClaudeHaiku => "Claude Haiku",
                ModelEconomico => "Modelo Base EIIBD",
                _ => modeloTecnico
            };
        }
    }
}
