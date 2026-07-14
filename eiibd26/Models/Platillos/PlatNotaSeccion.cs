using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Una sección de una nota clínica, en orden ("¿Qué es?", "¿Qué suele pasar?", "Antes de eliminarlos"…).
    /// El título de la sección intermedia cambia según el caso; por eso es dato, no código.
    /// Hijo puro: muere con su nota (CASCADE, calca la DB).
    /// Convención de <see cref="Contenido"/>: una viñeta por línea si empieza con "- ".
    /// </summary>
    public class PlatNotaSeccion
    {
        [Key]
        public int Id { get; set; }

        public int NotaClinicaId { get; set; }

        public int Orden { get; set; }

        public string? Titulo { get; set; }

        public string? Contenido { get; set; }

        public virtual PlatNotaClinica? Nota { get; set; }
    }
}
