using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Validacion
{
    [Table("ValidacionesContenidoProfesional")]
    public class ValidacionContenidoProfesional
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public TipoContenidoValidado TipoContenido { get; set; }

        [Required]
        public int ContenidoId { get; set; }

        [Required]
        [MaxLength(450)]
        public string UsuarioMedicoId { get; set; } = "";

        [MaxLength(800)]
        public string? Comentario { get; set; }

        [Required]
        public EstadoValidacion Estado { get; set; } = EstadoValidacion.Validado;

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

        public DateTime? ActualizadoEn { get; set; }

        [MaxLength(450)]
        public string? ModeradoPorId { get; set; }

        public DateTime? FechaModeracion { get; set; }

        [MaxLength(500)]
        public string? NotaModeracion { get; set; }
    }
}
