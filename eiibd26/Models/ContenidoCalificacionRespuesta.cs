using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Representa la tabla dbo.contenidosCalificacion_Respuestas
    /// </summary>
    [Table("contenidosCalificacion_Respuestas", Schema = "dbo")]
    public class ContenidoCalificacionRespuesta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("idUsuario")]
        public Guid IdUsuario { get; set; }

        [Column("idContenidoRespuesta")]
        public int IdContenidoRespuesta { get; set; }

        [Column("digito")]
        public int Digito { get; set; }

        [Column("Eliminado")]
        public bool Eliminado { get; set; }

        [Column("fechaCreado")]
        public DateTime FechaCreado { get; set; }

        [Column("fechaModificado")]
        public DateTime FechaModificado { get; set; }

        [Column("fechaEliminado")]
        public DateTime FechaEliminado { get; set; }

        [ForeignKey("IdContenidoRespuesta")]
        public virtual ContenidoRespuesta ContenidoRespuesta { get; set; }
    }
}