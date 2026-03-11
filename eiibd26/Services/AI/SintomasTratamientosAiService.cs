using eiibd26.Configuration;
using eiibd26.Models.Glossary;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Twilio.TwiML.Voice;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Implementación del servicio de generación de descripciones para síntomas y tratamientos
    /// </summary>
    public class SintomasTratamientosAiService : ISintomasTratamientosAiService
    {
        private readonly HttpClient _httpClient;
        private readonly AiAnswerConfiguration _config;
        private readonly ILogger<SintomasTratamientosAiService> _logger;

        public SintomasTratamientosAiService(
            IHttpClientFactory httpClientFactory,
            IOptions<AiAnswerConfiguration> config,
            ILogger<SintomasTratamientosAiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("AnthropicClient");
            _config = config.Value;
            _logger = logger;
        }

        public async Task<(string Descripcion, bool RelacionEII)> GenerarDescripcionSintomaAsync(
            string nombreSintoma, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombreSintoma))
                throw new ArgumentException("El nombre del síntoma no puede estar vacío", nameof(nombreSintoma));

            var systemPrompt = BuildSintomaSystemPrompt();
            var userPrompt = $@"Ahora genera el contenido para el siguiente síntoma: {nombreSintoma}

ADEMÁS, al final del texto:

1. Determina si este síntoma tiene relación documentada con la Enfermedad Inflamatoria Intestinal (EII - Crohn o Colitis Ulcerosa).

2. Si tiene relación, explica brevemente POR QUÉ o CÓMO se relaciona con la EII (en 1-2 frases cortas).

3. SOLO si tienes certeza de fuentes específicas donde esta relación está documentada, menciónalas. Si NO estás seguro, NO inventes fuentes.

IMPORTANTE: Responde en texto plano, SIN usar markdown (sin **, sin *, sin #).

Formato de respuesta al final:
RELACIÓN EII: [SÍ o NO]
NIVEL RELACIÓN EII: [SOLO si respondiste SÍ → escribe exactamente una de estas palabras: Directa, Indirecta o Secundaria. Si respondiste NO, escribe 'No aplica']
EXPLICACIÓN EII: [Si respondiste SÍ, explica la relación en 1-2 frases. Si respondiste NO, escribe 'No aplica']
RAZONAMIENTO GLOSARIO: [Frase clínica breve (máx 200 caracteres) para mostrar en el glosario. Si NO hay relación, escribe 'No aplica']
FUENTES: [SOLO si tienes certeza de fuentes específicas, menciónalas. Si no estás seguro, escribe 'No especificadas']

Guía de niveles (solo para NIVEL RELACIÓN EII):
- Directa: síntoma que aparece como manifestación directa o extraintestinal documentada de la EII.
- Indirecta: síntoma frecuente en EII pero no exclusivo ni patognomónico.
- Secundaria: síntoma que aparece como consecuencia de tratamientos o complicaciones de la EII.
Si hay duda → elige Indirecta.

Cuando describas la relación con EII:
-NO expliques mecanismos biológicos.
- NO uses frases como causa, provoca, debido a.
- Usa lenguaje observacional: algunas personas reportan.
- Siempre aclara que puede ocurrir por otras razones. "
;

            return await CallClaudeApiAsync(systemPrompt, userPrompt, cancellationToken);
        }

        public async Task<(string Descripcion, bool RelacionEII, string? NombreTraducido)> GenerarDescripcionTratamientoAsync(
            string nombreTratamiento, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombreTratamiento))
                throw new ArgumentException("El nombre del tratamiento no puede estar vacío", nameof(nombreTratamiento));

            var systemPrompt = BuildTratamientoSystemPrompt();
            var userPrompt = $@"Ahora genera el contenido para el siguiente tratamiento: {nombreTratamiento}

IMPORTANTE PRIMERO: Si el nombre del tratamiento está en inglés u otro idioma, tradúcelo al español. Mantén los nombres comerciales entre paréntesis tal como están.

Ejemplo: 
- Entrada: 'Prednisone (Deltasone, Prednisone Intensol)'
- Traducción: 'Prednisona (Deltasone, Prednisone Intensol)'

ADEMÁS, al final del texto:

1. Determina si este tratamiento tiene relación documentada con la Enfermedad Inflamatoria Intestinal (EII - Crohn o Colitis Ulcerosa).

2. Si tiene relación, explica brevemente POR QUÉ o CÓMO se usa en EII (en 1-2 frases cortas).

3. SOLO si tienes certeza de fuentes específicas donde esta relación está documentada, menciónalas. Si NO estás seguro, NO inventes fuentes.

IMPORTANTE: Responde en texto plano, SIN usar markdown (sin **, sin *, sin #).

Formato de respuesta al final:
NOMBRE ESPAÑOL: [Nombre del tratamiento en español. Si ya está en español, escribe el mismo nombre]
RELACIÓN EII: [SÍ o NO]
NIVEL RELACIÓN EII: [SOLO si respondiste SÍ → escribe exactamente una de estas palabras: Directa, Indirecta o Secundaria. Si respondiste NO, escribe 'No aplica']
EXPLICACIÓN EII: [Si respondiste SÍ, explica la relación en 1-2 frases. Si respondiste NO, escribe 'No aplica']
RAZONAMIENTO GLOSARIO: [Frase clínica breve (máx 200 caracteres) para mostrar en el glosario. Si NO hay relación, escribe 'No aplica']
FUENTES: [SOLO si tienes certeza de fuentes específicas, menciónalas. Si no estás seguro, escribe 'No especificadas']

Guía de niveles (solo para NIVEL RELACIÓN EII):
- Directa: tratamiento aprobado o indicado específicamente para la EII.
- Indirecta: tratamiento frecuentemente usado en EII pero no exclusivo (ej: analgésicos, antidiarreicos).
- Secundaria: tratamiento que se usa para manejar efectos secundarios o complicaciones del tratamiento de EII.
Si hay duda → elige Indirecta.

Cuando describas la relación con EII:
-NO expliques mecanismos biológicos.
- NO uses frases como causa, provoca, debido a.
- Usa lenguaje observacional: algunas personas reportan.
- Siempre aclara que puede ocurrir por otras razones. "
;

            return await CallClaudeApiTratamientoAsync(systemPrompt, userPrompt, cancellationToken);
        }

        private async Task<(string Descripcion, bool RelacionEII, string? NombreTraducido)> CallClaudeApiTratamientoAsync(
            string systemPrompt, 
            string userPrompt, 
            CancellationToken cancellationToken)
        {
            var (descripcion, relacionEII) = await CallClaudeApiAsync(systemPrompt, userPrompt, cancellationToken);
            return (descripcion, relacionEII, _lastNombreTraducido);
        }

        private async Task<(string Descripcion, bool RelacionEII)> CallClaudeApiAsync(
            string systemPrompt, 
            string userPrompt, 
            CancellationToken cancellationToken)
        {
            if (!_config.Enabled)
                throw new InvalidOperationException("El servicio de IA está deshabilitado");

            if (string.IsNullOrWhiteSpace(_config.AnthropicApiKey))
                throw new InvalidOperationException("La clave API de Anthropic no está configurada");

            try
            {
                var requestBody = new
                {
                    model = _config.Model,
                    max_tokens = _config.MaxTokens,
                    temperature = _config.Temperature,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = userPrompt }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Generando descripción IA. Model: {Model}", _config.Model);

                var response = await _httpClient.PostAsync("messages", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Error en Claude API: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Error en API de Claude: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent);

                if (responseData?.Content == null || responseData.Content.Length == 0)
                    throw new InvalidOperationException("La respuesta de la IA está vacía");

                var fullText = responseData.Content[0].Text ?? string.Empty;

                // Extraer descripción, relación EII y explicación
                var (descripcion, relacionEII) = ParseResponse(fullText);

                _logger.LogInformation("Descripción generada exitosamente. RelacionEII: {RelacionEII}, Explicación: {Explicacion}", 
                    relacionEII, _lastExplicacionEII);

                return (descripcion, relacionEII);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout al generar descripción IA");
                throw new TimeoutException("La generación de la descripción excedió el tiempo límite", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al generar descripción IA");
                throw;
            }
        }

        // Propiedades públicas para que el controller pueda acceder
        public string UltimaExplicacionEII => _lastExplicacionEII;
        public string UltimasFuentes => _lastFuentes;
        public string? UltimoNombreTraducido => _lastNombreTraducido;

        private (string Descripcion, bool RelacionEII) ParseResponse(string fullText)
        {
            // Buscar línea "NOMBRE ESPAÑOL: [texto]" (para tratamientos)
            var matchNombre = Regex.Match(fullText, @"\*?\*?NOMBRE ESPAÑOL:\*?\*?\s*(.+?)(?=\r?\n|$)", RegexOptions.IgnoreCase);

            // Buscar línea "RELACIÓN EII: SÍ/NO" (con o sin markdown)
            var matchRelacion = Regex.Match(fullText, @"\*?\*?RELACIÓN EII:\*?\*?\s*(SÍ|SI|NO)", RegexOptions.IgnoreCase);

            // Buscar línea "EXPLICACIÓN EII: [texto]" (con o sin markdown)
            var matchExplicacion = Regex.Match(fullText, @"\*?\*?EXPLICACIÓN EII:\*?\*?\s*(.+?)(?=\r?\n\r?\n|\r?\n\*?\*?FUENTES:|\r?\n\*?\*?RELACIÓN EII:|\r?\n$|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // ⭐ NUEVO: Buscar línea "FUENTES: [texto]" (con o sin markdown)
            var matchFuentes = Regex.Match(fullText, @"\*?\*?FUENTES:\*?\*?\s*(.+?)(?=\r?\n\r?\n|\r?\n$|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            bool relacionEII = false;
            string explicacionEII = "";
            string fuentes = "";
            string? nombreTraducido = null;
            string descripcion = fullText;

            // Log para debugging
            _logger.LogDebug("Texto completo recibido de Claude: {FullText}", fullText);

            // ⭐ PROCESAR NOMBRE TRADUCIDO (para tratamientos)
            if (matchNombre.Success)
            {
                var nombreRaw = matchNombre.Groups[1].Value.Trim();
                _logger.LogDebug("Nombre traducido encontrado: {Nombre}", nombreRaw);

                if (!string.IsNullOrWhiteSpace(nombreRaw))
                {
                    nombreTraducido = nombreRaw
                        .Replace("**", "")
                        .Replace("*", "")
                        .Trim();
                }
            }

            // Procesar RELACIÓN EII
            if (matchRelacion.Success)
            {
                relacionEII = matchRelacion.Groups[1].Value.ToUpperInvariant() is "SÍ" or "SI";

                _logger.LogDebug("Relación EII encontrada: {RelacionEII}", relacionEII ? "SÍ" : "NO");

                // Encontrar el inicio de la sección de metadatos (puede ser NOMBRE ESPAÑOL o RELACIÓN EII)
                int metadataStart = matchNombre.Success && matchNombre.Index < matchRelacion.Index 
                    ? matchNombre.Index 
                    : matchRelacion.Index;

                // Si hay explicación, usarla; si no, usar texto por defecto
                if (matchExplicacion.Success)
                {
                    var explicacionRaw = matchExplicacion.Groups[1].Value.Trim();
                    _logger.LogDebug("Explicación raw encontrada: {Explicacion}", explicacionRaw);

                    if (!string.IsNullOrWhiteSpace(explicacionRaw) && 
                        explicacionRaw.ToLower() != "no aplica")
                    {
                        // Limpiar markdown de la explicación
                        explicacionEII = explicacionRaw
                            .Replace("**", "")
                            .Replace("*", "")
                            .Trim();
                    }
                    else if (relacionEII)
                    {
                        explicacionEII = "Síntoma/tratamiento documentado en pacientes con EII";
                    }
                    else
                    {
                        explicacionEII = "No se encontró relación documentada con EII";
                    }
                }
                else if (relacionEII)
                {
                    explicacionEII = "Síntoma/tratamiento documentado en pacientes con EII";
                    _logger.LogWarning("No se encontró explicación en el texto, usando valor por defecto");
                }
                else
                {
                    explicacionEII = "No se encontró relación documentada con EII";
                }

                // ⭐ NUEVO: Procesar FUENTES
                if (matchFuentes.Success)
                {
                    var fuentesRaw = matchFuentes.Groups[1].Value.Trim();
                    _logger.LogDebug("Fuentes raw encontradas: {Fuentes}", fuentesRaw);

                    // Solo guardar si son fuentes reales, no valores por defecto
                    if (!string.IsNullOrWhiteSpace(fuentesRaw) && 
                        fuentesRaw.ToLower() != "no aplica" &&
                        fuentesRaw.ToLower() != "no especificadas" &&
                        fuentesRaw.ToLower() != "no disponible" &&
                        !fuentesRaw.Contains("No hay fuentes", StringComparison.OrdinalIgnoreCase))
                    {
                        // Limpiar markdown de las fuentes
                        fuentes = fuentesRaw
                            .Replace("**", "")
                            .Replace("*", "")
                            .Trim();
                    }
                }

                // ⭐ NO AGREGAR VALORES POR DEFECTO - Si no hay fuentes verificadas, dejar vacío
                if (string.IsNullOrWhiteSpace(fuentes))
                {
                    _logger.LogInformation("No se proporcionaron fuentes específicas verificadas");
                }

                // Remover toda la sección de metadatos de la descripción
                descripcion = fullText.Substring(0, metadataStart).Trim();
            }
            else
            {
                // Si no se encontró el patrón, usar valor por defecto
                explicacionEII = "No se pudo determinar la relación con EII";
                fuentes = ""; // ⭐ NO AGREGAR FUENTES FALSAS
                _logger.LogWarning("No se encontró el patrón 'RELACIÓN EII:' en la respuesta");
            }

            _logger.LogInformation("Resultado del parsing - RelacionEII: {RelacionEII}, Explicación: {Explicacion}, Fuentes: {Fuentes}, NombreTraducido: {Nombre}", 
                relacionEII, explicacionEII, fuentes, nombreTraducido ?? "No proporcionado");

            // ⭐ Extraer NIVEL RELACIÓN EII
            var matchNivel = Regex.Match(fullText, @"\*?\*?NIVEL RELACIÓN EII:\*?\*?\s*(Directa|Indirecta|Secundaria)", RegexOptions.IgnoreCase);
            MedicalRelationType? nivelRelacion = null;
            if (matchNivel.Success && relacionEII)
            {
                nivelRelacion = matchNivel.Groups[1].Value.Trim().ToLowerInvariant() switch
                {
                    "directa"   => MedicalRelationType.Directa,
                    "indirecta" => MedicalRelationType.Indirecta,
                    "secundaria" => MedicalRelationType.Secundaria,
                    _ => null
                };
                _logger.LogInformation("Nivel relación EII encontrado: {Nivel}", nivelRelacion);
            }
            // Si RelacionEII=true pero nivel no encontrado → default Indirecta
            else if (relacionEII)
            {
                nivelRelacion = MedicalRelationType.Indirecta;
                _logger.LogWarning("No se encontró NIVEL RELACIÓN EII, usando Indirecta por defecto");
            }

            // ⭐ Extraer RAZONAMIENTO GLOSARIO
            var matchRazonamiento = Regex.Match(fullText, @"\*?\*?RAZONAMIENTO GLOSARIO:\*?\*?\s*(.+?)(?=\r?\n|$)", RegexOptions.IgnoreCase);
            string? razonamiento = null;
            if (matchRazonamiento.Success)
            {
                var raw = matchRazonamiento.Groups[1].Value.Trim()
                    .Replace("**", "").Replace("*", "").Trim();
                if (!string.IsNullOrWhiteSpace(raw) &&
                    !raw.Equals("No aplica", StringComparison.OrdinalIgnoreCase))
                {
                    razonamiento = raw.Length > 500 ? raw[..500] : raw;
                    _logger.LogInformation("Razonamiento glosario encontrado: {Razonamiento}", razonamiento);
                }
            }

            // Guardar la explicación, fuentes, nombre traducido, nivel y razonamiento
            _lastExplicacionEII = explicacionEII;
            _lastFuentes        = fuentes;
            _lastNombreTraducido = nombreTraducido;
            _lastNivelRelacion  = nivelRelacion;
            _lastRazonamiento   = razonamiento;

            return (descripcion, relacionEII);
        }

        // Variables temporales para pasar datos al controller
        private string _lastExplicacionEII = "";
        private string _lastFuentes = "";
        private string? _lastNombreTraducido = null;
        private MedicalRelationType? _lastNivelRelacion = null;
        private string? _lastRazonamiento = null;

        public MedicalRelationType? UltimoNivelRelacion => _lastNivelRelacion;
        public string? UltimoRazonamiento => _lastRazonamiento;

        private string BuildSintomaSystemPrompt()
        {
            return @"Actúa como redactor de contenido médico orientado a pacientes no como médico ni como enciclopedia clínica.

Tu objetivo es describir síntomas en lenguaje sencillo para ayudar a las personas a reconocer y expresar lo que sienten, SIN diagnosticar ni explicar enfermedades en profundidad.

Reglas obligatorias:
• Usa lenguaje claro y cotidiano (nivel lectura 6–8 grado).
• NO expliques mecanismos biológicos complejos.
• NO menciones tratamientos.
• NO sugieras diagnósticos.
• NO listes causas específicas graves.
• Describe solo la EXPERIENCIA del síntoma.
• Mantén tono neutral y tranquilizador.
• Máximo 120 palabras totales.

Estructura EXACTA:

Nombre del síntoma (traducción simple)

¿Qué es?
Explicación breve en 1–2 frases centradas en cómo se siente.

¿Cómo puede sentirse?
• 4 ejemplos cotidianos reales del paciente.

Importante
Aclara que es un síntoma y no un diagnóstico y que requiere evaluación médica si persiste.";
        }

        private string BuildTratamientoSystemPrompt()
        {
            return @"Actúa como redactor de contenido médico orientado a pacientes, no como médico ni como enciclopedia clínica.

Tu objetivo es describir tratamientos en lenguaje sencillo para ayudar a las personas a entender qué son y cómo funcionan de forma general, SIN profundizar en mecanismos biológicos complejos.

Reglas obligatorias:
• Usa lenguaje claro y cotidiano (nivel lectura 6–8 grado).
• NO expliques mecanismos biológicos muy complejos.
• Describe el PROPÓSITO y FORMA DE USO general.
• NO sugieras que es la solución definitiva.
• Mantén tono neutral y tranquilizador.
• Máximo 120 palabras totales.

Estructura EXACTA:

Nombre del tratamiento (traducción simple si es necesario)

¿Qué es?
Explicación breve en 1–2 frases sobre su propósito.

¿Cómo se usa?
• 3-4 ejemplos de formas comunes de administración/uso.

Importante
Aclara que debe seguir recomendaciones médicas y que todo tratamiento requiere supervisión profesional.";
        }

        #region DTOs for Claude API

        private class ClaudeApiResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public ContentItem[]? Content { get; set; }
        }

        private class ContentItem
        {
            [System.Text.Json.Serialization.JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        #endregion
    }
}
