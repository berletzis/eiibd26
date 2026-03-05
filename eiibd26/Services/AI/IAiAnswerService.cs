using eiibd26.Models;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Servicio para generar respuestas educativas automáticas usando IA
    /// </summary>
    public interface IAiAnswerService
    {
        /// <summary>
        /// Genera una respuesta educativa para una pregunta específica
        /// </summary>
        /// <param name="pregunta">La pregunta para la cual generar la respuesta</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <param name="contextoDinamico">Contexto adicional opcional (relaciones, síntomas, etc.)</param>
        /// <returns>Texto de la respuesta generada por IA</returns>
        Task<string> GenerarRespuestaAsync(Pregunta pregunta, CancellationToken cancellationToken = default, string? contextoDinamico = null);

        /// <summary>
        /// Verifica si el servicio de IA está disponible y configurado correctamente
        /// </summary>
        /// <returns>True si está disponible, false en caso contrario</returns>
        Task<bool> IsAvailableAsync();
    }
}
