using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models
{
    public class Etiqueta
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(250)]
        public string Nombre { get; set; }

        [Required, MaxLength(250)]
        public string NombreCanonico { get; set; }

        [Required, MaxLength(50)]
        public string Tipo { get; set; } // 'condicion' | 'sintoma' | 'tratamiento'

        [MaxLength(200)]
        public string Fuente { get; set; } // 'diccionario', 'UMLS', 'manual', etc.

        public string Sinonimos { get; set; } // JSON array o texto con sinónimos

        public bool Eliminado { get; set; } = false; // soft-delete

        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
    }
}
