namespace eiibd26.Services.Calidad
{
    /// <summary>
    /// GRIS: Generador de Revisión Inteligente y Sugerencias.
    /// Evaluación editorial con IA — rúbrica de 7 aspectos. Bajo demanda por artículo.
    /// NO aplica filtros médicos ni disclaimers (es evaluación editorial, no respuesta a paciente).
    /// </summary>
    public interface IGrisEvaluadorService
    {
        /// <summary>
        /// Evalúa editorialmente un contenido con Claude y persiste el resultado en ContenidoCalidad.
        /// Llamar siempre en try-catch — un fallo de IA nunca debe romper la página.
        /// </summary>
        Task<GrisEvaluacionDto> EvaluarAsync(int contenidoId);
    }
}
