using eiibd26.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Constructor de prompts para IA optimizado para preguntas médicas sobre EII
    /// Diseñado para ser extensible con RAG en el futuro
    /// </summary>
    public class AiPromptBuilder : IAiPromptBuilder
    {
        private readonly ILogger<AiPromptBuilder> _logger;

        public AiPromptBuilder(ILogger<AiPromptBuilder> logger)
        {
            _logger = logger;
        }

        public string BuildSystemPrompt()
        {
            return @"Eres un miembro experimentado de una comunidad de apoyo sobre Enfermedades Inflamatorias Intestinales (EII): Enfermedad de Crohn y Colitis Ulcerosa. Tu rol es ofrecer información educativa, empática y contextualizada, NO eres un médico ni debes actuar como uno.

OBJETIVO DE LA RESPUESTA:
Ayudar a la persona a:
- comprender conceptos generales,
- sentirse acompañada,
- reducir confusión inicial,
- obtener orientación educativa segura.

REGLAS OBLIGATORIAS:
1. NO diagnostiques ni interpretes síntomas como conclusiones médicas
2. NO sugieras cambios en medicamentos o tratamientos
3. Usa lenguaje probabilístico: ""Algunas personas con EII..."", ""En general..."", ""Cada caso es diferente...""
4. Evita certezas: NO digas ""Esto es normal para ti"" o ""Tu tratamiento está funcionando""
5. NO menciones marcas, instituciones específicas ni asociaciones. Di ""recursos locales"" o ""asociaciones en tu país""
6. Para impacto económico: ""Algunas familias necesitan adaptarse a aspectos prácticos...""
7. Para apoyo emocional: ""Algunas personas encuentran útil hablar con profesionales de apoyo...""
8. USA SOLO UN AVISO al final (no dupliques advertencias)

ESTRUCTURA:
1. Validación empática breve (1 línea)
2. Información educativa específica a la pregunta
3. Cuándo consultar al médico (2-3 puntos concretos)
4. Sugerencias prácticas seguras (opcional)

LONGITUD: Máximo 300 tokens. Sé directo y útil.

FORMATO: Markdown simple con negritas moderadas.

CIERRE OBLIGATORIO (copia exacto):
⚠️ *Importante:* Esta información es educativa y no sustituye la evaluación de un profesional de salud. Consulta siempre con tu médico o especialista para decisiones médicas.";
        }

        public string BuildUserPrompt(Pregunta pregunta, string? contextoDinamico = null)
        {
            // Log RAW input antes de procesar
            _logger.LogInformation(
                "📥 [Prompt Builder] RAW INPUT - PreguntaId={PreguntaId}, Título='{Titulo}', Cuerpo RAW (primeros 200 chars)='{CuerpoRaw}'",
                pregunta.Id, 
                pregunta.Titulo ?? "[VACÍO]",
                string.IsNullOrWhiteSpace(pregunta.Cuerpo) ? "[VACÍO]" : 
                    (pregunta.Cuerpo.Length > 200 ? pregunta.Cuerpo.Substring(0, 200) + "..." : pregunta.Cuerpo));

            // Limpiar HTML del cuerpo para enviar texto plano a la IA
            var cuerpoLimpio = StripHtml(pregunta.Cuerpo ?? "");

            // Truncar si es muy largo para evitar exceder límites de tokens
            const int maxLength = 2000;
            if (cuerpoLimpio.Length > maxLength)
            {
                cuerpoLimpio = cuerpoLimpio.Substring(0, maxLength) + "... [texto truncado]";
                _logger.LogWarning(
                    "⚠️ [Prompt Builder] Cuerpo truncado a {MaxLength} caracteres para pregunta {PreguntaId}",
                    maxLength, pregunta.Id);
            }

            _logger.LogInformation(
                "📝 [Prompt Builder] AFTER CLEAN - PreguntaId={PreguntaId}, Cuerpo limpio length={CleanLength}, Contenido='{Contenido}'",
                pregunta.Id, cuerpoLimpio.Length,
                string.IsNullOrWhiteSpace(cuerpoLimpio) ? "[VACÍO DESPUÉS DE LIMPIAR]" : 
                    (cuerpoLimpio.Length > 200 ? cuerpoLimpio.Substring(0, 200) + "..." : cuerpoLimpio));

            // Construir prompt más directo y específico
            var userPrompt = $@"Pregunta sobre {(pregunta.Titulo?.Contains("Crohn", StringComparison.OrdinalIgnoreCase) == true ? "Crohn" : pregunta.Titulo?.Contains("Colitis", StringComparison.OrdinalIgnoreCase) == true ? "Colitis Ulcerosa" : "EII")}:

**{pregunta.Titulo}**

{cuerpoLimpio}";

            // Futuro: Aquí se inyectará contexto de RAG
            if (!string.IsNullOrWhiteSpace(contextoDinamico))
            {
                if (contextoDinamico.Length > 500)
                {
                    contextoDinamico = contextoDinamico.Substring(0, 500) + "...";
                }
                userPrompt += $@"

Contexto: {contextoDinamico}";
                _logger.LogInformation("📋 [Prompt Builder] Contexto dinámico agregado");
            }

            userPrompt += @"

Responde de forma específica a esta situación, siendo empático y educativo. Enfócate en lo que la persona preguntó.";

            _logger.LogInformation(
                "📤 [Prompt Builder] PROMPT FINAL GENERADO ({Length} chars):\n{Prompt}",
                userPrompt.Length, userPrompt);

            return userPrompt;
        }

        /// <summary>
        /// Limpia HTML y devuelve texto plano para enviar a la IA
        /// </summary>
        private string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("⚠️ [StripHtml] Input HTML es nulo o vacío");
                return "";
            }

            _logger.LogDebug("🧹 [StripHtml] Input HTML (primeros 100 chars): {Html}", 
                html.Length > 100 ? html.Substring(0, 100) + "..." : html);

            // Remover scripts y styles
            var noScript = Regex.Replace(html, @"<script[\s\S]*?>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            noScript = Regex.Replace(noScript, @"<style[\s\S]*?>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);

            // Remover tags HTML
            var text = Regex.Replace(noScript, @"<[^>]+>", " ");

            // Decodificar entidades HTML
            text = System.Net.WebUtility.HtmlDecode(text);

            // Normalizar espacios
            text = Regex.Replace(text, @"\s+", " ").Trim();

            _logger.LogDebug("🧹 [StripHtml] Output text (primeros 100 chars): {Text}", 
                text.Length > 100 ? text.Substring(0, 100) + "..." : text);

            return text;
        }
    }
}
