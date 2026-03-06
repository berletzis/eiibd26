using eiibd26.Models;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Servicio para buscar respuestas similares ya existentes en la base de datos
    /// Evita consumo innecesario de tokens de IA
    /// </summary>
    public interface IQuestionCacheService
    {
        /// <summary>
        /// Busca si ya existe una respuesta de IA para una pregunta similar
        /// </summary>
        /// <param name="pregunta">Pregunta a buscar</param>
        /// <param name="umbralSimilitud">Umbral de similitud (0.0 a 1.0). Default: 0.85</param>
        /// <returns>Respuesta existente si se encuentra, null si no hay coincidencias</returns>
        Task<Respuesta?> BuscarRespuestaSimilarAsync(Pregunta pregunta, double umbralSimilitud = 0.85);

        /// <summary>
        /// Calcula similitud entre dos preguntas usando algoritmo de distancia de Levenshtein
        /// </summary>
        /// <param name="pregunta1">Primera pregunta</param>
        /// <param name="pregunta2">Segunda pregunta</param>
        /// <returns>Puntaje de similitud entre 0.0 (diferentes) y 1.0 (idénticas)</returns>
        double CalcularSimilitud(string pregunta1, string pregunta2);
    }
}
