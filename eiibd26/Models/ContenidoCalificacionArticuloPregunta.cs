using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Representa la tabla dbo.contenidosCalificacion_ArticulosPreguntas
    /// </summary>
    [Table("contenidosCalificacion_ArticulosPreguntas", Schema = "dbo")]
    public class ContenidoCalificacionArticuloPregunta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("idUsuario")]
        public Guid IdUsuario { get; set; }

        [Column("idContenido")]
        public int IdContenido { get; set; }

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

        [ForeignKey("IdContenido")]
        public virtual Contenido Contenido { get; set; }
    }
}