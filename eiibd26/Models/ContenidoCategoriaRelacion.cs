using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Representa la tabla dbo.contenidosCategoriasRelacion
    /// </summary>
    
    public class ContenidoCategoriaRelacion
    {
        [Key]
    
        public int Sequence { get; set; }

    
        public int IdContenido { get; set; }

    
        public Guid? IdPerfil { get; set; }

    
        public int? IdEmpresa { get; set; }

    
        public int? IdCategoria { get; set; }

    
        public Guid UsuarioCreacion { get; set; }

    
        public Guid UsuarioModificacion { get; set; }

    
        public DateTime FechaCreacion { get; set; }

    
        public DateTime FechaModificacion { get; set; }

    
        public bool Borrado { get; set; }

        // New: flag to mark this relation as the principal category for the content
        // NOTE: DB schema must include a corresponding nullable bit column (EsPrincipal). You will run the migration/SQL.
        public bool? EsPrincipal { get; set; }

        /* Navigation */
        [ForeignKey("IdCategoria")]
        public virtual ContenidoCategoria Categoria { get; set; }

        [ForeignKey("IdContenido")]
        public virtual Contenido Contenido { get; set; }
    }
}