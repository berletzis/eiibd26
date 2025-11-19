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

        public int ContenidoId { get; set; }

        public Guid PreguntaId { get; set; }

        public Guid? UsuarioCreacion { get; set; }

        public Guid? UsuarioModificacion { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime FechaModificacion { get; set; }

        
        public bool Borrado { get; set; }

        [ForeignKey("ContenidoId")]
        public virtual Contenido Contenido { get; set; }

        // Navigation to Pregunta left as optional if Pregunta entity exists
        // [ForeignKey("PreguntaId")]
        // public virtual Pregunta Pregunta { get; set; }
    }
}