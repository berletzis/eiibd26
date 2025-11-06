using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace eiibd26.Models
{
    public class ZonaHoraria
    {
        [Key]
        public int idZone { get; set; }

        [Required]
        [StringLength(40)]
        public string zoneinfo { get; set; }

        [Required]
        [StringLength(16)]
        public string offset { get; set; }

        [StringLength(16)]
        public string summer { get; set; }

        [Required]
        [StringLength(2)]
        public string PaisCodigo { get; set; }

        [Required]
        [StringLength(6)]
        public string cicode { get; set; }

        [StringLength(6)]
        public string cicodesummer { get; set; }

        public int offset_seconds { get; set; }

        // Propiedad de navegación (si tienes el modelo Pais, descomenta lo siguiente)
        // [ForeignKey(nameof(PaisCodigo))]
        // public virtual Pais Pais { get; set; }
    }

}