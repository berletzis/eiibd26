using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models
{
    public class Voto
    {
        [Key]
        public Guid Id { get; set; }

        // 'pregunta' | 'respuesta' (max length 20)
        public string EntidadTipo { get; set; } = null!;

        // Id of the question or answer being voted
        public Guid EntidadId { get; set; }

        // FK to AspNetUsers.Id (stored as uniqueidentifier)
        public Guid UsuarioId { get; set; }

        // +1 or -1
        public short Valor { get; set; }

        // soft-delete flag
        public bool Eliminado { get; set; }

        // momento en que se registró el evento (se usa como timestamp general)
        public DateTimeOffset Fecha { get; set; }

        public DateTimeOffset? FechaCreacion { get; set; }
        public DateTimeOffset? FechaModificacion { get; set; }

        // Navigation
        public virtual ApplicationUser? Usuario { get; set; }
    }
}