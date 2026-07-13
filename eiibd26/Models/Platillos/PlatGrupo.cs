using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>Qué ES el alimento (lácteo, verdura, pescado…). Catálogo con baja lógica (Activo).</summary>
    public class PlatGrupo
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;

        public virtual ICollection<PlatIngrediente> Ingredientes { get; set; } = new List<PlatIngrediente>();
    }
}
