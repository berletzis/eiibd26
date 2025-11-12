using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models
{
    public class Pregunta
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UsuarioId { get; set; } // FK a AspNetUsers.Id

        [Required, MaxLength(300)]
        public string Titulo { get; set; }

        [Required]
        public string Cuerpo { get; set; }

        public bool Resuelta { get; set; } = false;

        public bool Eliminado { get; set; } = false; // soft-delete

        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? FechaModificacion { get; set; }

        // Navegación
        public List<Respuesta> Respuestas { get; set; } = new List<Respuesta>();
        public List<PreguntaEtiqueta> PreguntaEtiquetas { get; set; } = new List<PreguntaEtiqueta>();
    }
}
