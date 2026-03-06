using eiibd26.Models;
using eiibd26.Models.AI;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Servicio de enrutamiento inteligente de preguntas a modelos de IA
    /// Decide automáticamente qué modelo usar según complejidad y riesgo
    /// </summary>
    public interface IAIModelRouter
    {
        /// <summary>
        /// Procesa una pregunta y genera una respuesta usando el modelo apropiado
        /// </summary>
        /// <param name="pregunta">Pregunta a procesar</param>
        /// <param name="contextoDinamico">Contexto adicional opcional</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Respuesta de IA con metadata completa</returns>
        Task<AIResponse> AskAsync(Pregunta pregunta, string? contextoDinamico = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clasifica el nivel de complejidad de una pregunta
        /// </summary>
        /// <param name="question">Texto de la pregunta</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Nivel de complejidad detectado</returns>
        Task<QuestionLevel> ClassifyQuestionAsync(string question, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detecta si una pregunta tiene alto riesgo médico
        /// </summary>
        /// <param name="question">Texto de la pregunta</param>
        /// <returns>True si se detecta alto riesgo</returns>
        bool DetectHighRisk(string question);
    }
}
