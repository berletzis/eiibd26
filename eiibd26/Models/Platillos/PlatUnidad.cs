using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>Unidad de medida (pieza, taza, g, ml, cda, al gusto…). Catálogo con baja lógica.</summary>
    public class PlatUnidad
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public bool Activo { get; set; } = true;
    }
}
