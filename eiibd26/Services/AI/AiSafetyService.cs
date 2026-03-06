using eiibd26.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Servicio de seguridad para validar respuestas de IA
    /// Implementa filtros de contenido médico peligroso
    /// </summary>
    public class AiSafetyService : IAiSafetyService
    {
        private readonly AiAnswerConfiguration _config;
        private readonly ILogger<AiSafetyService> _logger;

        private const string DISCLAIMER = "\n\n---\n\n⚠️ **Aviso Importante:** Esta respuesta es informativa y educativa. No reemplaza la consulta con un profesional médico. Siempre consulta con tu gastroenterólogo o equipo de salud antes de tomar decisiones sobre tu tratamiento.";

        private const string FALLBACK_RESPONSE = @"**Información General sobre EII**

Las Enfermedades Inflamatorias Intestinales (EII) son condiciones crónicas que afectan el sistema digestivo. Cada paciente tiene una experiencia única con su condición.

**Recomendaciones Generales:**
- Mantén comunicación constante con tu equipo médico
- Lleva un registro de tus síntomas
- Sigue el plan de tratamiento prescrito
- No modifiques tu medicación sin consultar a tu médico

**Cuándo Buscar Atención Médica:**
- Síntomas nuevos o que empeoran
- Sangrado rectal significativo
- Dolor abdominal intenso
- Fiebre persistente
- Deshidratación

**Recursos:**
- ACCU (Confederación de Asociaciones de Crohn y Colitis Ulcerosa)
- Tu equipo de gastroenterología
- Grupos de apoyo locales

Por favor, consulta con un profesional médico para obtener orientación específica sobre tu situación.";

        public AiSafetyService(
            IOptions<AiAnswerConfiguration> config,
            ILogger<AiSafetyService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        public bool ValidarContenido(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
            {
                _logger.LogWarning("❌ [Safety] Contenido vacío recibido para validación");
                return false;
            }

            var contenidoNormalizado = contenido.ToLowerInvariant();

            _logger.LogDebug("🔍 [Safety] Validando contenido (longitud: {Length})", contenido.Length);

            // Verificar frases prohibidas configuradas
            foreach (var frase in _config.ForbiddenPhrases)
            {
                if (contenidoNormalizado.Contains(frase.ToLowerInvariant()))
                {
                    _logger.LogWarning(
                        "🚫 [Safety] Content BLOCKED by forbidden phrase: '{Phrase}'",
                        frase);
                    return false;
                }
            }

            // ⭐ MEJORADO: Patrones más específicos que permiten educación pero bloquean prescripción directa
            var patronesPeligrosos = new[]
            {
                // 1. DOSAGE ADVICE DIRECTO (solo imperativo, no educativo)
                @"\b(debes|debe|tienes\s+que)\s+(aumenta|aumentar|incrementa|incrementar|reduce|reducir|modifica|modificar|cambia|cambiar|ajusta|ajustar)\s+.{0,40}(dosis|mg|cantidad|medicamento)",

                // 2. MEDICATION CESSATION IMPERATIVO (solo comandos directos)
                @"\b(debes|debe|tienes\s+que)\s+(suspende|suspender|deja\s+de|dejar\s+de|para|parar|detén|detener)\s+.{0,50}(tomar|medicamento|tratamiento)",

                // 3. DIAGNOSIS STATEMENTS DEFINITIVOS (solo afirmaciones directas)
                @"\b(definitivamente\s+tienes|con\s+certeza\s+padeces|claramente\s+sufres)\s+(de\s+)?(cáncer|tumor|enfermedad\s+terminal)",

                // 4. SPECIFIC DOSAGE INSTRUCTIONS CON IMPERATIVO
                @"\b(toma|tomar|consume|consumir)\s+\d+\s*(mg|miligramo|tableta|pastilla|cápsula)\s+(de|cada|al\s+día)",

                // 5. TREATMENT MODIFICATIONS IMPERATIVAS
                @"\b(suspende|cambia|modifica|aumenta|reduce)\s+(tu|el|la)\s+(medicamento|tratamiento|dosis)\s+(inmediatamente|ahora|ya|sin\s+consultar)"
            };

            foreach (var patron in patronesPeligrosos)
            {
                try
                {
                    if (Regex.IsMatch(contenidoNormalizado, patron, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
                    {
                        _logger.LogWarning(
                            "🚫 [Safety] Content BLOCKED by pattern: {Pattern}",
                            patron.Substring(0, Math.Min(80, patron.Length)));

                        // Log snippet del contenido que coincidió (primeros 200 chars para debugging)
                        var snippet = contenido.Length > 200 ? contenido.Substring(0, 200) + "..." : contenido;
                        _logger.LogDebug("📝 [Safety] Blocked content snippet: {Snippet}", snippet);

                        return false;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    _logger.LogWarning(
                        "⏱️ [Safety] Regex timeout on pattern (blocking for safety): {Pattern}",
                        patron.Substring(0, Math.Min(50, patron.Length)));
                    return false; // Fail safe: block on timeout
                }
            }

            _logger.LogInformation("✅ [Safety] Content validation PASSED (length: {Length})", contenido.Length);
            return true;
        }

        public string ObtenerRespuestaFallback()
        {
            _logger.LogWarning("⚠️ [Safety] Retornando respuesta de seguridad FALLBACK (algo salió mal)");
            return FALLBACK_RESPONSE + DISCLAIMER;
        }

        public string AgregarDisclaimer(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return contenido;

            // Si ya tiene alguna versión del disclaimer, no duplicar
            if (contenido.Contains("⚠️ **Aviso Importante:**") || 
                contenido.Contains("⚠️ *Importante:*") ||
                contenido.Contains("⚠️") && contenido.Contains("Importante"))
            {
                _logger.LogDebug("[Safety] Disclaimer ya presente, no se duplicará");
                return contenido;
            }

            _logger.LogDebug("[Safety] Agregando disclaimer a la respuesta");
            return contenido + DISCLAIMER;
        }
    }
}
