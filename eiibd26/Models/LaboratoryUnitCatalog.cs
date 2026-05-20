using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models
{
    public sealed class LaboratoryUnitCatalog
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Abreviatura { get; set; } = string.Empty;

        public int Orden { get; set; }
    }
}
