using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace eiibd26.Models
{
    public class PreguntaEtiqueta
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PreguntaId { get; set; }

        [Required]
        public Guid EtiquetaId { get; set; } // FK a Etiquetas.Id

        public bool Eliminado { get; set; } = false; // soft-delete

        public DateTimeOffset FechaRelacion { get; set; } = DateTimeOffset.UtcNow;
    }
}
