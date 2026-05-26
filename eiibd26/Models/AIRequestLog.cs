using System.ComponentModel.DataAnnotations;
using eiibd26.Models.AI;

namespace eiibd26.Models
{
    /// <summary>
    /// Registro de solicitudes procesadas por el sistema NINA Router
    /// Para análisis de costos y métricas de uso
    /// </summary>
    public class AIRequestLog
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// ID de la pregunta procesada
        /// </summary>
        public Guid PreguntaId { get; set; }

        /// <summary>
        /// Pregunta completa (título + cuerpo)
        /// </summary>
        public string QuestionText { get; set; } = string.Empty;

        /// <summary>
        /// Nivel de complejidad detectado
        /// </summary>
        public QuestionLevel Level { get; set; }

        /// <summary>
        /// Indica si se detectó alto riesgo
        /// </summary>
        public bool HighRisk { get; set; }

        /// <summary>
        /// Modelo de IA utilizado
        /// </summary>
        public string ModelUsed { get; set; } = string.Empty;

        /// <summary>
        /// Tiempo de procesamiento en milisegundos
        /// </summary>
        public double ProcessingTimeMs { get; set; }

        /// <summary>
        /// Timestamp de la solicitud
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Indica si la operación fue exitosa
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mensaje de error (si aplica)
        /// </summary>
        public string? ErrorMessage { get; set; }

        // Navegación
        public Pregunta? Pregunta { get; set; }
    }
}
