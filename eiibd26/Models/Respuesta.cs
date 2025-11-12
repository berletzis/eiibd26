using System;
using System.Collections.Generic;

namespace eiibd26.Models
{
    public class Respuesta
    {
        public Guid Id { get; set; }
        public Guid PreguntaId { get; set; }

        // New: optional parent response id for nested replies
        public Guid? ParentRespuestaId { get; set; }

        public Guid UsuarioId { get; set; }
        public string Cuerpo { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
        public bool EsAceptada { get; set; }
        public bool Eliminado { get; set; }

        // Navigation properties (if used)
        public Pregunta Pregunta { get; set; }
        public Respuesta ParentRespuesta { get; set; }
        public ICollection<Respuesta> Children { get; set; }
    }
}