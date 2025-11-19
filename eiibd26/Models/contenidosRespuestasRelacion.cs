using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Representa la tabla dbo.contenidosRespuestasRelacion
    /// </summary>
    [Table("contenidosRespuestasRelacion", Schema = "dbo")]
    public class ContenidoRespuestaRelacion
    {
        [Key]
        [Column("Sequence")]
        public int Sequence { get; set; }

        [Column("idContenido")]
        public int ContenidoId { get; set; }

        [Column("RespuestaId")]
        public Guid RespuestaId { get; set; }

        [Column("UsuarioCreacion")]
        public Guid? UsuarioCreacion { get; set; }

        [Column("UsuarioModificacion")]
        public Guid? UsuarioModificacion { get; set; }

        [Column("FechaCreacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("FechaModificacion")]
        public DateTime FechaModificacion { get; set; }

        [Column("Borrado")]
        public bool Borrado { get; set; }

        [ForeignKey("ContenidoId")]
        public virtual Contenido Contenido { get; set; }

        // Navigation to Respuesta left as optional if Respuesta entity exists
        // [ForeignKey("RespuestaId")]
        // public virtual Respuesta Respuesta { get; set; }
    }
}