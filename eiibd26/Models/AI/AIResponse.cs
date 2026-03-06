namespace eiibd26.Models.AI
{
    /// <summary>
    /// Respuesta generada por el sistema de IA con metadata
    /// </summary>
    public class AIResponse
    {
        /// <summary>
        /// Contenido de la respuesta generada
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Autor de la respuesta (siempre "NINA")
        /// </summary>
        public string Author { get; set; } = "NINA";

        /// <summary>
        /// Modelo de IA utilizado para generar la respuesta
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Nivel de complejidad detectado en la pregunta
        /// </summary>
        public QuestionLevel Level { get; set; }

        /// <summary>
        /// Indica si se detectó alto riesgo médico
        /// </summary>
        public bool HighRisk { get; set; }

        /// <summary>
        /// Tiempo de procesamiento en segundos
        /// </summary>
        public double ProcessingTimeSeconds { get; set; }

        /// <summary>
        /// Timestamp de generación
        /// </summary>
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
