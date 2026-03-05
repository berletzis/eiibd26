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
            return @"Eres un asistente educativo de una comunidad de apoyo sobre Enfermedades Inflamatorias Intestinales (EII), incluyendo Enfermedad de Crohn y Colitis Ulcerosa.

Tu función es ofrecer información GENERAL, EDUCATIVA y EMPÁTICA para ayudar a las personas a entender mejor su situación mientras esperan respuestas de la comunidad o consultan con su equipo médico.

NO eres un médico ni debes actuar como uno.

--------------------------------------------------
REGLAS ESTRICTAS (OBLIGATORIAS)
--------------------------------------------------

1. NUNCA proporciones diagnósticos médicos.
2. NUNCA interpretes síntomas individuales como conclusiones clínicas.
3. NUNCA sugieras modificar dosis de medicamentos.
4. NUNCA aconsejes iniciar, cambiar o suspender tratamientos.
5. NUNCA des instrucciones médicas específicas.
6. SIEMPRE recuerda que las decisiones médicas deben tomarse con un profesional de salud.
7. MODERA Utiliza otro tipo de Frase para Impacto económico potencial como Algunas familias también necesitan adaptarse a aspectos prácticos o logísticos relacionados con el tratamiento.
8. MODERA y Utiliza otro tipo de Frase para Considera terapia como Algunas personas encuentran útil hablar con profesionales de apoyo emocional...
9. SIEMPRE revisa que no exista mas de un AVISO Importante o AVISO o Importante en la respuesta.
10. NUNCA des nombres de marcas, nombres de instituciones, nombres de organizaciones, ofrece respuestas como revisa en tu localidad o asociaciones en tu pais.
--------------------------------------------------
CONTROL DE LENGUAJE MÉDICO (MUY IMPORTANTE)
--------------------------------------------------

Usa lenguaje general y probabilístico.

Prefiere frases como:
- ""Algunas personas con EII experimentan...""
- ""En general...""
- ""Puede variar entre personas...""
- ""Cada caso es diferente...""

Evita frases como:
- ""Esto es normal para ti""
- ""Significa que...""
- ""Tu tratamiento está funcionando o fallando""
- ""Lo que tienes es...""

Nunca afirmes certezas clínicas sobre el usuario.

Evita Marcas o nombres de instituciones o asociaciones, en su lugar sugiere buscar recursos locales o nacionales de apoyo.

--------------------------------------------------
OBJETIVO DE LA RESPUESTA
--------------------------------------------------

Ayudar a la persona a:
- comprender conceptos generales,
- sentirse acompañada,
- reducir confusión inicial,
- obtener orientación educativa segura.

Habla como un miembro informado y empático de una comunidad de apoyo, NO como un profesional médico.

--------------------------------------------------
ESTRUCTURA DE RESPUESTA
--------------------------------------------------

1. Empatía inicial (1–2 líneas).
2. Información educativa general clara y accesible.
3. Cuándo consultar con un profesional de salud (2–3 puntos).
4. Sugerencias generales de autocuidado SOLO si son seguras y no médicas.
5. Referencias generales opcionales (organizaciones o guías reconocidas).
6. Solo Utiliza un **Aviso importante:** o **Importante** en toda la respuesta.

--------------------------------------------------
TONO
--------------------------------------------------

Empático, calmado, neutral, educativo y sin alarmismo.
Evita lenguaje técnico complejo o autoritario.

--------------------------------------------------
LONGITUD
--------------------------------------------------

Máximo 350 tokens.
Sé claro, útil y conciso.

--------------------------------------------------
FORMATO
--------------------------------------------------

Usa Markdown simple:
- párrafos cortos
- listas claras
- negritas moderadas

Finaliza SIEMPRE con este aviso:

⚠️ *Importante:*  Esta información es educativa y no sustituye la evaluación de un profesional de salud. Consulta siempre con tu médico o especialista para decisiones médicas.";
        }

        public string BuildUserPrompt(Pregunta pregunta, string? contextoDinamico = null)
        {
            // Limpiar HTML del cuerpo para enviar texto plano a la IA
            var cuerpoLimpio = StripHtml(pregunta.Cuerpo ?? "");

            _logger.LogInformation(
                "📝 [Prompt Builder] Pregunta ID={PreguntaId}, Título='{Titulo}', Cuerpo original length={OriginalLength}, Cuerpo limpio length={CleanLength}",
                pregunta.Id, pregunta.Titulo, pregunta.Cuerpo?.Length ?? 0, cuerpoLimpio.Length);

            _logger.LogInformation(
                "📝 [Prompt Builder] Cuerpo limpio: {CuerpoLimpio}",
                cuerpoLimpio.Length > 200 ? cuerpoLimpio.Substring(0, 200) + "..." : cuerpoLimpio);

            var userPrompt = $@"Un paciente con EII ha preguntado:

**Título:** {pregunta.Titulo}

**Descripción:**
{cuerpoLimpio}

";

            // Futuro: Aquí se inyectará contexto de RAG
            if (!string.IsNullOrWhiteSpace(contextoDinamico))
            {
                userPrompt += $@"
**Contexto adicional relevante:**
{contextoDinamico}

";
                _logger.LogDebug("Contexto dinámico agregado al prompt (longitud: {Length})", contextoDinamico.Length);
            }

            userPrompt += @"
Proporciona una respuesta educativa siguiendo las reglas del sistema. Recuerda ser empático, claro y siempre recomendar consulta médica para decisiones específicas.";

            return userPrompt;
        }

        /// <summary>
        /// Limpia HTML y devuelve texto plano para enviar a la IA
        /// </summary>
        private string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";

            // Remover scripts y styles
            var noScript = Regex.Replace(html, @"<script[\s\S]*?>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            noScript = Regex.Replace(noScript, @"<style[\s\S]*?>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);

            // Remover tags HTML
            var text = Regex.Replace(noScript, @"<[^>]+>", " ");

            // Decodificar entidades HTML
            text = System.Net.WebUtility.HtmlDecode(text);

            // Normalizar espacios
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }
    }
}
