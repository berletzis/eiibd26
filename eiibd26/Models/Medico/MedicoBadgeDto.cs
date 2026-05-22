namespace eiibd26.Models.Medico;

public class MedicoBadgeDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string ComoObtenerlo { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public bool Obtenido { get; set; }
    public DateTime? FechaObtenido { get; set; }
}
