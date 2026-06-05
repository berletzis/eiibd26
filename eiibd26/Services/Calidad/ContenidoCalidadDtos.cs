namespace eiibd26.Services.Calidad
{
    public class ResultadosGuardadosDto
    {
        public List<ContenidoCalidadDto> Resultados { get; init; } = new();
        public DateTime UltimoAnalisis { get; init; }
    }

    public enum GravedadSenal { Critica, Mejorable }

    public enum NivelSemaforo
    {
        Critico = 0,
        Mejorable = 1,
        Ok = 2
    }

    public record SenalCalidad(string Codigo, string Descripcion, GravedadSenal Gravedad);

    public class ContenidoCalidadDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public int? EstadoPublicacion { get; set; }
        public DateTime FechaCreado { get; set; }
        public List<SenalCalidad> Senales { get; set; } = new();
        public NivelSemaforo NivelSemaforo { get; set; }
        public List<int> DuplicadoDeIds { get; set; } = new();

        // GRIS editorial — resumen para la lista (detalles se cargan al evaluar)
        public bool GrisEvaluado { get; set; }
        public int? GrisPuntajeGlobal { get; set; }
        public DateTime? GrisFechaEvaluacion { get; set; }
    }
}
