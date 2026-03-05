using eiibd26.Models;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Servicio para construir prompts optimizados para IA
    /// Diseñado para ser extensible con RAG en el futuro
    /// </summary>
    public interface IAiPromptBuilder
    {
        /// <summary>
        /// Construye el prompt del sistema con las instrucciones médicas y de seguridad
        /// </summary>
        /// <returns>Prompt del sistema</returns>
        string BuildSystemPrompt();

        /// <summary>
        /// Construye el prompt del usuario basado en la pregunta
        /// </summary>
        /// <param name="pregunta">La pregunta del usuario</param>
        /// <param name="contextoDinamico">Contexto opcional (para RAG futuro)</param>
        /// <returns>Prompt del usuario</returns>
        string BuildUserPrompt(Pregunta pregunta, string? contextoDinamico = null);
    }
}
