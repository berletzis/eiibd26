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

        // ===== AI FIELDS =====
        /// <summary>
        /// Indica si esta respuesta fue generada por IA
        /// </summary>
        public bool EsIA { get; set; } = false;

        /// <summary>
        /// Modelo de IA utilizado (ej: "claude-sonnet-4.5")
        /// </summary>
        [MaxLength(100)]
        public string? ModeloIA { get; set; }

        /// <summary>
        /// Indica si la respuesta debe mostrarse colapsada por defecto
        /// </summary>
        public bool EsColapsada { get; set; } = false;

        /// <summary>
        /// Puntuación de la respuesta (votos positivos - negativos)
        /// </summary>
        public int Puntuacion { get; set; } = 0;
        // ===== END AI FIELDS =====

        [ForeignKey(nameof(PreguntaId))]
        public Pregunta Pregunta { get; set; }

        [ForeignKey(nameof(ParentRespuestaId))]
        public Respuesta Parent { get; set; }
    }
}