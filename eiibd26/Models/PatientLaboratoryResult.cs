using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class PatientLaboratoryResult
    {
        [Key]
        public int Id { get; set; }

        public Guid PatientId { get; set; }

        public int LaboratoryTypeId { get; set; }

        [MaxLength(500)]
        public string? ResultValue { get; set; }

        [MaxLength(50)]
        public string? ResultUnit { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime? ResultDate { get; set; }

        public int? LaboratoryUnitCatalogId { get; set; }

        public int? CondicionUsuarioId { get; set; }
        public int? SintomaUsuarioId { get; set; }
        public int? TratamientoUsuarioId { get; set; }

        // Auditoría
        public DateTime FechaCreado { get; set; }
        public Guid? CreadoPor { get; set; }
        public DateTime? FechaModificado { get; set; }
        public Guid? ModificadoPor { get; set; }
        public DateTime? FechaEliminado { get; set; }
        public Guid? EliminadoPor { get; set; }
        public bool Eliminado { get; set; }

        // Navegación
        [ForeignKey(nameof(PatientId))]
        public virtual ApplicationUser? Patient { get; set; }

        [ForeignKey(nameof(LaboratoryTypeId))]
        public virtual LaboratoryType? LaboratoryType { get; set; }

        [ForeignKey(nameof(LaboratoryUnitCatalogId))]
        public virtual LaboratoryUnitCatalog? LaboratoryUnit { get; set; }

        [ForeignKey(nameof(CondicionUsuarioId))]
        public virtual condicionUsuario? CondicionUsuario { get; set; }

        [ForeignKey(nameof(SintomaUsuarioId))]
        public virtual sintomasUsuario? SintomaUsuario { get; set; }

        [ForeignKey(nameof(TratamientoUsuarioId))]
        public virtual tratamientoUsuario? TratamientoUsuario { get; set; }
    }
}
