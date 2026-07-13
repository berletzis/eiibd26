using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// El alimento base (nombre único, minúscula, singular). El paciente filtra por ingrediente.
    /// Catálogo con baja lógica (Activo). Los atributos INTRÍNSECOS viven en PlatIngredienteAtributo.
    /// </summary>
    public class PlatIngrediente
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public int GrupoId { get; set; }
        public string? NotasEII { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }

        [ForeignKey(nameof(GrupoId))]
        public virtual PlatGrupo? Grupo { get; set; }

        /// <summary>Atributos intrínsecos del ingrediente (siempre-son: gluten, picante, cítrico…).</summary>
        public virtual ICollection<PlatIngredienteAtributo> Atributos { get; set; } = new List<PlatIngredienteAtributo>();
    }
}
