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

        [Required, MaxLength(255)]
        public string Slug { get; set; } = "";  // ← AGREGAR ESTA LÍNEA

        public bool Resuelta { get; set; } = false;

        public bool Eliminado { get; set; } = false;
        public bool Deshabilitado { get; set; } = false;
        [MaxLength(300)]
        public string? MotivoModeracion { get; set; }

        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? FechaModificacion { get; set; }

        // ===== AI FIELDS =====
        /// <summary>
        /// Indica si ya se generó una respuesta de IA para esta pregunta
        /// </summary>
        public bool TieneRespuestaIA { get; set; } = false;

        /// <summary>
        /// Fecha en que se generó la respuesta de IA
        /// </summary>
        public DateTimeOffset? FechaGeneracionIA { get; set; }
        // ===== END AI FIELDS =====

        // Navegación
        public List<Respuesta> Respuestas { get; set; } = new List<Respuesta>();
        public List<PreguntaEtiqueta> PreguntaEtiquetas { get; set; } = new List<PreguntaEtiqueta>();
        public List<PreguntaCondicion> PreguntaCondiciones { get; set; } = new List<PreguntaCondicion>();
        public List<PreguntaSintoma> PreguntaSintomas { get; set; } = new List<PreguntaSintoma>();
        public List<PreguntaTratamiento> PreguntaTratamientos { get; set; } = new List<PreguntaTratamiento>();
    }
}
