using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Voto de utilidad genérico sobre un destino del módulo (platillo o ingrediente).
    /// Tabla PROPIA polimórfica (TipoDestino+DestinoId, mismo patrón que PlatNotaClinica) —
    /// NO reusa ArticleRating. Un voto por usuario por destino (UNIQUE), cambiable.
    /// Valor: 1 = útil, -1 = no útil. `idUsuario` es referencia lógica a Identity (sin FK física).
    /// </summary>
    public class PlatCalificacion
    {
        [Key]
        public int Id { get; set; }

        /// <summary>'Platillo' | 'Ingrediente'.</summary>
        public string TipoDestino { get; set; } = "";

        /// <summary>Id de PlatPlatillo o PlatIngrediente según TipoDestino.</summary>
        public int DestinoId { get; set; }

        public Guid idUsuario { get; set; }

        /// <summary>1 = me fue útil · -1 = no me fue útil.</summary>
        public short Valor { get; set; }

        public DateTime Fecha { get; set; }
    }
}
