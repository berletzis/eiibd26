using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Voto de utilidad de un usuario sobre la ficha de un ingrediente ("¿Puedo comer queso?").
    /// Tabla PROPIA del módulo — NO reusa ArticleRating. Un voto por usuario por ingrediente
    /// (UNIQUE IngredienteId+idUsuario), cambiable. Valor: 1 = útil, -1 = no útil.
    /// `idUsuario` es referencia lógica a Identity (sin FK física), por aislamiento.
    /// </summary>
    public class PlatIngredienteCalificacion
    {
        [Key]
        public int Id { get; set; }

        public int IngredienteId { get; set; }

        public Guid idUsuario { get; set; }

        /// <summary>1 = me fue útil · -1 = no me fue útil.</summary>
        public short Valor { get; set; }

        public DateTime Fecha { get; set; }
    }
}
