using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Una referencia bibliográfica de una nota clínica, en orden. Se pinta como bloque "Referencias"
    /// al final de la nota, con rel="noopener". Hijo puro: muere con su nota (CASCADE, calca la DB).
    /// </summary>
    public class PlatNotaReferencia
    {
        [Key]
        public int Id { get; set; }

        public int NotaClinicaId { get; set; }

        public int Orden { get; set; }

        public string Titulo { get; set; } = "";

        public string? Url { get; set; }

        public virtual PlatNotaClinica? Nota { get; set; }
    }
}
