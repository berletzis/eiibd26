namespace eiibd26.Services.Calidad
{
    /// <summary>
    /// Analiza señales de calidad sobre Contenidos (extensible a otros tipos).
    /// Análisis bajo demanda — no llamar en cada carga de página.
    /// </summary>
    public interface IContenidoCalidadService
    {
        /// <summary>
        /// Analiza el rango [skip, skip+take) de contenidos no eliminados.
        /// Carga textos de TODOS para detección de duplicados, pero evalúa señales solo del batch.
        /// Cada petición es rápida (≤10 items, pre-filtro Jaccard elimina el 99%+ del Levenshtein).
        /// </summary>
        Task<CalidadBatchResultDto> AnalizarBatchAsync(int skip, int take);

        /// <summary>Analiza todos los contenidos en una sola llamada (solo para tests/uso interno).</summary>
        Task<List<ContenidoCalidadDto>> AnalizarTodosAsync();

        /// <summary>
        /// Lee los resultados guardados en ContenidoCalidad (join con Contenidos, filtrando eliminados).
        /// Devuelve null si no hay análisis previo.
        /// </summary>
        Task<ResultadosGuardadosDto?> ObtenerResultadosGuardadosAsync();
    }
}
