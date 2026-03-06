using eiibd26.Models;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Servicio para detectar preguntas similares y evitar llamadas redundantes a la IA
    /// </summary>
    public interface ISimilarQuestionDetector
    {
        /// <summary>
        /// Busca una pregunta similar que ya tenga respuesta de IA
        /// </summary>
        /// <param name="pregunta">Pregunta a buscar</param>
        /// <param name="umbralSimilitud">Umbral de similitud (0.0 a 1.0), por defecto 0.80 (80%)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Respuesta de IA de pregunta similar, o null si no encuentra coincidencias</returns>
        Task<Respuesta?> BuscarRespuestaSimilarAsync(
            Pregunta pregunta, 
            double umbralSimilitud = 0.80, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calcula la similitud entre dos preguntas (0.0 = diferentes, 1.0 = idénticas)
        /// </summary>
        double CalcularSimilitud(string texto1, string texto2);
    }
}
