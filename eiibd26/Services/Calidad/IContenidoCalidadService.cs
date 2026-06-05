namespace eiibd26.Services.Calidad
{
    /// <summary>
    /// Analiza señales de calidad sobre Contenidos (extensible a otros tipos).
    /// Análisis bajo demanda — no llamar en cada carga de página.
    /// </summary>
    public interface IContenidoCalidadService
    {
        /// <summary>
        /// Evalúa todos los contenidos no eliminados y devuelve la lista
        /// con señales de calidad y nivel de semáforo por cada uno.
        /// O(n²) para duplicados — solo invocar bajo demanda.
        /// </summary>
        Task<List<ContenidoCalidadDto>> AnalizarTodosAsync();
    }
}
