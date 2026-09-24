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

        /// <summary>
        /// Artículo del nombre para el copy público ("¿Toleras <b>la</b> leche?"): el/la/los/las.
        /// NULL = sin capturar → la vista usa una frase neutra, nunca adivina el género.
        /// Columna SQL-directa (SQL/2026-09-24-platillos-ingrediente-articulo-y-acentos.sql, deploy-gate).
        /// </summary>
        public string? Articulo { get; set; }

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
