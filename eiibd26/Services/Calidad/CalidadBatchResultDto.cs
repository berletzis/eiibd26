namespace eiibd26.Services.Calidad
{
    public class CalidadBatchResultDto
    {
        /// <summary>Total de contenidos NO eliminados (para calcular progreso en el front).</summary>
        public int Total { get; set; }
        /// <summary>Resultados del rango solicitado (skip/take).</summary>
        public List<ContenidoCalidadDto> Items { get; set; } = new();
    }
}
