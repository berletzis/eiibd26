namespace eiibd26.Services.AI
{
    /// <summary>
    /// Servicio de seguridad para validar respuestas de IA
    /// Detecta contenido potencialmente peligroso o inapropiado
    /// </summary>
    public interface IAiSafetyService
    {
        /// <summary>
        /// Valida si una respuesta de IA es segura para mostrar a usuarios
        /// </summary>
        /// <param name="contenido">El texto de la respuesta a validar</param>
        /// <returns>True si es seguro, false si debe ser reemplazado por fallback</returns>
        bool ValidarContenido(string contenido);

        /// <summary>
        /// Obtiene una respuesta de seguridad genérica cuando el contenido falla la validación
        /// </summary>
        /// <returns>Texto de respuesta segura predefinida</returns>
        string ObtenerRespuestaFallback();

        /// <summary>
        /// Agrega el disclaimer obligatorio a una respuesta
        /// </summary>
        /// <param name="contenido">Contenido original</param>
        /// <returns>Contenido con disclaimer agregado</returns>
        string AgregarDisclaimer(string contenido);
    }
}
