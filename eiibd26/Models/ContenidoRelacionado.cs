using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Representa la tabla dbo.contenidosRelacionados
    /// </summary>
    public class ContenidoRelacionado
    {
        [Key]
        public int Sequence { get; set; }

        public int IdContenido { get; set; }

        // Debe coincidir con la columna en la BD: IdContenidoRelacionado
        public int IdContenidoRelacionado { get; set; }

        public int Tipo { get; set; }

        // Nullable porque en la BD la columna puede contener NULL
        public string? Descripcion { get; set; }

        public DateTime? FechaCreacion { get; set; }

        public DateTime? FechaModificacion { get; set; }

        public Guid? UsuarioCreacion { get; set; }

        public Guid? UsuarioModificacion { get; set; }

        public bool Borrado { get; set; }

        /* Navigation properties */
        // Contenido de origen
        [ForeignKey("IdContenido")]
        public virtual Contenido? Contenido { get; set; }

        // Contenido relacionado (propiedad renombrada para evitar conflicto con el nombre de la clase)
        [ForeignKey("IdContenidoRelacionado")]
        public virtual Contenido? ContenidoRelacionadoItem { get; set; }
    }
}