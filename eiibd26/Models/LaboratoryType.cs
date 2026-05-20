using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class LaboratoryType
    {
        [Key]
        public int Id { get; set; }

        public int? ParentId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string? Slug { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }

        // Auditoría
        public DateTime FechaCreado { get; set; }
        public Guid? CreadoPor { get; set; }
        public DateTime? FechaModificado { get; set; }
        public Guid? ModificadoPor { get; set; }
        public DateTime? FechaEliminado { get; set; }
        public Guid? EliminadoPor { get; set; }
        public bool Eliminado { get; set; }

        // Navegación
        [ForeignKey(nameof(ParentId))]
        public virtual LaboratoryType? Parent { get; set; }
        public virtual ICollection<LaboratoryType> Children { get; set; } = new List<LaboratoryType>();
        public virtual ICollection<PatientLaboratoryResult> Results { get; set; } = new List<PatientLaboratoryResult>();
    }
}
