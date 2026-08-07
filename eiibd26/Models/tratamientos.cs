using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace eiibd26.Models
{
    public class tratamientos
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(250)]
        public string? nombre { get; set; }

        public int? idPadre { get; set; }

        public int? idIdioma { get; set; }

        [StringLength(50)]
        public string? icono { get; set; }

        // ===== NUEVOS CAMPOS PARA IA Y VALIDACIÓN =====
        public string? DescripcionIA { get; set; }

        [Display(Name = "Validado por IA")]
        public bool ValidadoIA { get; set; } = false;

        [Display(Name = "Validado por Humano")]
        public bool ValidadoHumano { get; set; } = false;

        public string? NombreSugeridoIA { get; set; }

        [Display(Name = "Relación con EII (texto)")]
        [StringLength(1000)]
        public string? RelacionEIIDescripcion { get; set; }

        [Display(Name = "Relación con EII detectada por IA")]
        public bool RelacionEII { get; set; } = false;

        [Display(Name = "Fuentes sugeridas por IA")]
        [StringLength(500)]
        public string? Fuentes { get; set; }

        public DateTime? FechaActualizacionIA { get; set; }

        // ===== TRIAGE DE LIMPIEZA (NINA) =====
        // Eje 1 — ¿es un tratamiento de verdad? Independiente de la relación con EII (eje 2):
        // un procedimiento real sin relación con EII es Válido, no basura.
        // NULL = no revisado · 1 = Válido · 2 = Basura · 3 = Dudoso (cola humana).
        [Display(Name = "Triage de limpieza")]
        public byte? RevisionLimpiezaEstado { get; set; }

        /// <summary>Confianza 0–1 de la clasificación. Solo se desactiva por encima del umbral.</summary>
        public decimal? RevisionLimpiezaConfianza { get; set; }

        [StringLength(400)]
        public string? RevisionLimpiezaMotivo { get; set; }

        public DateTime? RevisionLimpiezaFecha { get; set; }

        // ===== CAMPOS EXISTENTES =====
        public DateTime fechaEliminado { get; set; }
        public DateTime fechaModificado { get; set; }
        public DateTime fechaCreado { get; set; }
        public bool Eliminado { get; set; }

        // ===== NAVIGATION PROPERTIES =====
        public virtual ICollection<tratamientos> Hijos { get; set; }
        public virtual tratamientos Padre { get; set; }
        public virtual ICollection<tratamientoUsuario> TratamientosUsuario { get; set; }
        public virtual ICollection<TratamientosNotas> Notas { get; set; }
    }
}