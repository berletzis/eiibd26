using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Renglón de ingrediente dentro de un platillo. Contenido de autoría, SIN soft delete:
    /// al editar el platillo se hace delete-all + insert de sus renglones.
    /// Los atributos de USO (crudo/frito/en jugo en ESE platillo) viven en PlatPlatilloIngredienteAtributo.
    /// </summary>
    public class PlatPlatilloIngrediente
    {
        [Key]
        public int Id { get; set; }
        public int PlatilloId { get; set; }
        public int IngredienteId { get; set; }
        public string? TextoOriginal { get; set; }       // se copia tal cual de la fuente (trazabilidad)
        public decimal? Cantidad { get; set; }
        public int? UnidadId { get; set; }
        public bool EsAlGusto { get; set; }
        public string? NotaPreparacion { get; set; }

        [ForeignKey(nameof(PlatilloId))]
        public virtual PlatPlatillo? Platillo { get; set; }
        [ForeignKey(nameof(IngredienteId))]
        public virtual PlatIngrediente? Ingrediente { get; set; }
        [ForeignKey(nameof(UnidadId))]
        public virtual PlatUnidad? Unidad { get; set; }

        public virtual ICollection<PlatPlatilloIngredienteAtributo> Atributos { get; set; } = new List<PlatPlatilloIngredienteAtributo>();
    }
}
