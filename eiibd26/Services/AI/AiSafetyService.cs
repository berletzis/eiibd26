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
                _logger.LogWarning("Contenido vacío recibido para validación");
                return false;
            }

            var contenidoNormalizado = contenido.ToLowerInvariant();

            // Verificar frases prohibidas configuradas
            foreach (var frase in _config.ForbiddenPhrases)
            {
                if (contenidoNormalizado.Contains(frase.ToLowerInvariant()))
                {
                    _logger.LogWarning(
                        "[Safety] Content BLOCKED by forbidden phrase: {Phrase}",
                        frase);
                    return false;
                }
            }

            // ENHANCED: Patrones de seguridad usando regex (más exhaustivos)
            var patronesPeligrosos = new[]
            {
                // 1. DOSAGE ADVICE (comprehensive)
                @"\b(aumenta|aumentar|incrementa|incrementar|reduce|reducir|modifica|modificar|cambia|cambiar|ajusta|ajustar)\s+.{0,40}(dosis|mg|miligramo|gramo|cantidad|medicamento|fármaco|pastilla)",

                // 2. MEDICATION CESSATION (all variations)
                @"\b(suspende|suspender|deja\s+de|dejar\s+de|para|parar|detén|detener|elimina|eliminar|quita|quitar|pausa|pausar|interrump[ie]|cesar)\s+.{0,50}(tomar|medicamento|tratamiento|fármaco|medicina|pastilla|terapia)",

                // 3. DIAGNOSIS STATEMENTS (direct and implied)
                @"\b(tienes|tiene|padeces|padece|sufres|sufre|estás\s+diagnosticado|está\s+diagnosticado)\s+(de\s+)?(cáncer|tumor|neoplasia|carcinoma|enfermedad\s+grave|condición\s+severa|enfermedad\s+terminal)",

                // 4. DIAGNOSIS IMPLICATIONS (symptoms = disease)
                @"\b(estos|tus|sus)\s+(síntomas|signos)\s+(son|indican|sugieren|corresponden|apuntan|significan)\s+.{0,50}(diagnóstico|enfermedad|condición|problema|cáncer|tumor)",

                // 5. IMPERATIVE MEDICAL INSTRUCTIONS
                @"\b(debes|debe|tienes\s+que|tiene\s+que|deberías|debería|necesitas|necesita)\s+.{0,50}(tomar|suspender|aumentar|reducir|cambiar|modificar|consultar\s+sobre|probar)\s+.{0,30}(medicamento|dosis|tratamiento)",

                // 6. SPECIFIC DOSAGE INSTRUCTIONS
                @"\b\d+\s*(mg|miligramo|gramo|ml|mililitro|tableta|pastilla|cápsula)\s+(de|cada|al\s+día|por\s+día)",

                // 7. TREATMENT MODIFICATIONS
                @"\b(si\s+no\s+ves|si\s+no\s+sientes)\s+.{0,30}(mejoría|mejora|resultados|efecto)\s*.{0,30}(podrías|deberías|considera|intenta|prueba)\s+.{0,30}(suspender|cambiar|modificar|aumentar|reducir)",

                // 8. SELF-DIAGNOSIS
                @"\b(probablemente|posiblemente|quizás|tal\s+vez)\s+(tienes|tengas|sufres|padezcas)\s+.{0,30}(cáncer|tumor|enfermedad\s+grave|condición\s+seria)",

                // 9. URGENCY WITHOUT PROPER QUALIFIER
                @"\b(urgente|emergencia|grave|crítico|peligroso)\b(?!.*consult[ae].*médico)",

                // 10. MEDICATION NAMES + ACTION VERBS
                @"\b(mesalazina|azatioprina|infliximab|adalimumab|prednisona|cortisona|metotrexato)\s+.{0,30}(aumenta|reduce|suspende|cambia|modifica|para|deja)"
            };

            foreach (var patron in patronesPeligrosos)
            {
                try
                {
                    if (Regex.IsMatch(contenidoNormalizado, patron, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
                    {
                        _logger.LogWarning(
                            "[Safety] Content BLOCKED by pattern: {Pattern}",
                            patron.Substring(0, Math.Min(50, patron.Length)));
                        return false;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    _logger.LogWarning(
                        "[Safety] Regex timeout on pattern (blocking for safety): {Pattern}",
                        patron.Substring(0, Math.Min(50, patron.Length)));
                    return false; // Fail safe: block on timeout
                }
            }

            _logger.LogDebug("[Safety] Content validation PASSED");
            return true;
        }

        public string ObtenerRespuestaFallback()
        {
            _logger.LogInformation("Retornando respuesta de seguridad fallback");
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
