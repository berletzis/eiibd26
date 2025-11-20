using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class Respuesta
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PreguntaId { get; set; }

        [Required]
        public Guid UsuarioId { get; set; }

        [Required]
        public string Cuerpo { get; set; }

        public bool EsAceptada { get; set; }
        public bool Eliminado { get; set; }

        [Required]
        public DateTimeOffset FechaCreacion { get; set; }

        public DateTimeOffset? FechaModificacion { get; set; }

        public Guid? ParentRespuestaId { get; set; }

        [ForeignKey(nameof(PreguntaId))]
        public Pregunta Pregunta { get; set; }

        [ForeignKey(nameof(ParentRespuestaId))]
        public Respuesta Parent { get; set; }
    }
}