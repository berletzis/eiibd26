namespace eiibd26.Configuration
{
    /// <summary>
    /// Configuración para el servicio de respuestas de IA
    /// </summary>
    public class AiAnswerConfiguration
    {
        /// <summary>
        /// Clave API de Anthropic Claude
        /// </summary>
        public string AnthropicApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Modelo a utilizar (default: claude-haiku-4-5-20251001)
        /// </summary>
        public string Model { get; set; } = "claude-haiku-4-5-20251001";

        /// <summary>
        /// Temperatura para generación (0.0 = determinista, 1.0 = creativo)
        /// Rango recomendado para respuestas médicas: 0.2-0.3
        /// </summary>
        public double Temperature { get; set; } = 0.3;

        /// <summary>
        /// Máximo de tokens en la respuesta (para controlar costos)
        /// </summary>
        public int MaxTokens { get; set; } = 600;

        /// <summary>
        /// Timeout para llamadas a la API (en segundos)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// URL base de la API de Anthropic
        /// </summary>
        public string ApiBaseUrl { get; set; } = "https://api.anthropic.com/v1";

        /// <summary>
        /// Versión de la API de Anthropic
        /// </summary>
        public string ApiVersion { get; set; } = "2023-06-01";

        /// <summary>
        /// GUID del usuario "sistema" que será el autor de respuestas de IA
        /// Debe configurarse con un usuario válido en la base de datos
        /// </summary>
        public Guid SystemUserId { get; set; }

        /// <summary>
        /// Indica si el sistema de IA está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Palabras o frases prohibidas que activarán el fallback de seguridad
        /// </summary>
        public List<string> ForbiddenPhrases { get; set; } = new List<string>
        {
            // Dosage modifications
            "aumenta la dosis",
            "aumentar dosis",
            "incrementa dosis",
            "reduce la dosis",
            "reducir dosis",
            "modifica la dosis",
            "modificar dosis",
            "cambia la dosis",
            "cambiar dosis",
            "ajusta la dosis",
            "ajustar dosis",

            // Medication cessation
            "suspende el medicamento",
            "suspender medicamento",
            "deja de tomar",
            "dejar de tomar",
            "para el tratamiento",
            "parar el tratamiento",
            "elimina el medicamento",
            "quita el medicamento",
            "interrumpe el tratamiento",
            "pausar el tratamiento",

            // Diagnosis statements
            "tienes cáncer",
            "tiene cáncer",
            "padeces cáncer",
            "sufres de cáncer",
            "estás diagnosticado con",
            "está diagnosticado con",
            "te diagnostico con",
            "diagnóstico:",
            "diagnostico:",
            "tu diagnóstico es",

            // Specific medications + actions
            "aumenta mesalazina",
            "reduce azatioprina",
            "suspende infliximab",
            "para prednisona",
            "deja adalimumab",

            // Urgency without proper medical qualifier
            "es urgente que",
            "es emergencia",
            "crítico que",
            "peligroso que"
        };
    }
}
