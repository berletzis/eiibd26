namespace eiibd26.Services.Calidad
{
    public class GrisAspectoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int Puntaje { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }

    public class GrisCategoriaSugeridaDto
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Razon { get; set; } = string.Empty;
    }

    public class GrisEvaluacionDto
    {
        public int ContenidoId { get; set; }
        public bool Ok { get; set; }
        public string? Error { get; set; }
        public int PuntajeGlobal { get; set; }
        public List<GrisAspectoDto> Aspectos { get; set; } = new();
        public List<string> Sugerencias { get; set; } = new();
        public List<GrisCategoriaSugeridaDto> CategoriasSugeridas { get; set; } = new();
        public List<GrisCategoriaSugeridaDto> CategoriasAlerta { get; set; } = new();
        public DateTime FechaEvaluacion { get; set; }
    }
}
