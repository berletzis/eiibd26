namespace eiibd26.Models.AI
{
    /// <summary>
    /// Nivel de complejidad de una pregunta
    /// </summary>
    public enum QuestionLevel
    {
        /// <summary>
        /// Pregunta informativa general
        /// </summary>
        Simple,

        /// <summary>
        /// Requiere explicación contextual
        /// </summary>
        Medium,

        /// <summary>
        /// Incluye síntomas personales, medicamentos o decisiones médicas
        /// </summary>
        Complex
    }
}
