using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Representa la tabla dbo.contenidosPreguntasRelacion
    /// </summary>
    public class ContenidoPreguntaRelacion
    {
        [Key]
        public int Sequence { get; set; }

        // El nombre de la columna en la BD es `idContenido` (minúscula).
        // Mapeamos la propiedad C# a esa columna para que EF genere SQL correcto.
        [Column("idContenido")]
        public int ContenidoId { get; set; }

        public Guid PreguntaId { get; set; }

        public Guid? UsuarioCreacion { get; set; }

        public Guid? UsuarioModificacion { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime FechaModificacion { get; set; }

        public bool Borrado { get; set; }

        [ForeignKey("ContenidoId")]
        public virtual Contenido Contenido { get; set; }

        // Navigation to Pregunta (optional)
        // [ForeignKey("PreguntaId")]
        // public virtual Pregunta Pregunta { get; set; }
    }
}