using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    
    public class ContenidoRelacionado
    {
        [Key]
        
        public int Sequence { get; set; }

        
        public int IdContenido { get; set; }

        // Note: DB column name contains a typo idCotenidoRelacionado - keep it to match DB
        
        public int IdContenidoRelacionado { get; set; }

        
        public int Tipo { get; set; }

        
        public string Descripcion { get; set; }

        
        public DateTime FechaCreacion { get; set; }

        
        public DateTime FechaModificacion { get; set; }

        
        public Guid UsuarioCreacion { get; set; }

        
        public Guid UsuarioModificacion { get; set; }

        
        public bool Borrado { get; set; }

        /* Navigation */
        [ForeignKey("IdContenido")]
        public virtual Contenido Contenido { get; set; }

        // Renamed navigation property to avoid name clash with the enclosing type.
        // Keep ForeignKey attribute pointing to the DB FK property name.
        [ForeignKey("IdContenidoRelacionado")]
        public virtual Contenido ContenidosRelacionados { get; set; }
    }
}