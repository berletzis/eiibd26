using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class ContenidoCategoria
    {
        [Key]
        public int Sequence { get; set; }

        public int? CategoriaPadre { get; set; }

        // Nullable porque en la BD estas columnas pueden contener NULL
        public string? Clave { get; set; }

        public int? Orden { get; set; }
        public string? Nombre { get; set; }

        public string? Descripcion { get; set; }

        public string? Imagen { get; set; }

        public bool? Relevante { get; set; }

        public string? CategoriaSlug { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime FechaModificacion { get; set; }

        public Guid? UsuarioCreacion { get; set; }

        public Guid? UsuarioModificacion { get; set; }

        public bool Borrado { get; set; }

        /* Navigation */
        [ForeignKey("CategoriaPadre")]
        public virtual ContenidoCategoria? Padre { get; set; }
    }
}