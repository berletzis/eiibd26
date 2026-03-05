using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    [Table("PreguntaSintomas")]
    public class PreguntaSintoma
    {
        [Key] public Guid Id { get; set; }
        [Required] public Guid PreguntaId { get; set; }
        [Required] public int SintomaId { get; set; }
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(PreguntaId))]
        public virtual Pregunta Pregunta { get; set; }

        [ForeignKey(nameof(SintomaId))]
        public virtual sintomas Sintoma { get; set; }
    }
}