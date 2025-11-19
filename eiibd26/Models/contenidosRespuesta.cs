using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Representa la tabla dbo.contenidosRespuestas
    /// </summary>
    
    public class ContenidoRespuesta
    {
        [Key]
    
        public int Id { get; set; }

        // FK a contenidos.id
    
        public int ContenidoId { get; set; }

    
        public string Metas { get; set; }

    
        public string RespuestaTitulo { get; set; }

    
        public string RespuestaTextoC { get; set; }

    
        public string RespuestaTextoL { get; set; }

    
        public string Slug { get; set; }

   
        public string URLImagenPrincipal { get; set; }

   
        public bool? EsPopular { get; set; }

   
        public DateTime? FechaPublicacion { get; set; }

   
        public DateTime FechaEliminado { get; set; }

   
        public DateTime FechaModificado { get; set; }

   
        public DateTime FechaCreado { get; set; }

   
        public bool Eliminado { get; set; }

   
        public Guid IdAutor { get; set; }

   
        public string Autor { get; set; }

        /* Navigation properties */
        [ForeignKey("ContenidoId")]
        public virtual Contenido Contenido { get; set; }

        [ForeignKey("IdAutor")]
        public virtual Perfil AutorPerfil { get; set; }
    }
}