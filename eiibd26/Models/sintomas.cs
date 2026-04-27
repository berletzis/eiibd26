using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace eiibd26.Models
{
    /// <summary>Clasifica el síntoma para determinar qué campos de tracking aplican.</summary>
    public enum TipoSintomaEnum
    {
        /// <summary>Subjetivo / sistémico: acné, fatiga, ansiedad, dolor artritis. Captura Estado + Dolor.</summary>
        Subjetivo = 0,
        /// <summary>Medible (GI): diarrea, evacuaciones, sangrado. Captura Estado + Dolor + Frecuencia + Sangrado.</summary>
        Medible = 1
    }

    public class sintomas
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

        [Display(Name = "Relación con EII (texto)")]
        [StringLength(1000)]
        public string? RelacionEIIDescripcion { get; set; }

        [Display(Name = "Relación con EII detectada por IA")]
        public bool RelacionEII { get; set; } = false;

        [Display(Name = "Fuentes sugeridas por IA")]
        [StringLength(500)]
        public string? Fuentes { get; set; }

        public DateTime? FechaActualizacionIA { get; set; }

        // ===== CLASIFICACIÓN CLÍNICA =====
        /// <summary>
        /// Clasifica el síntoma para controlar qué campos de tracking se muestran y validan.
        /// 0 = Subjetivo (Estado + Dolor), 1 = Medible/GI (Estado + Dolor + Frecuencia + Sangrado).
        /// </summary>
        [Display(Name = "Tipo de síntoma")]
        public int TipoSintoma { get; set; } = (int)TipoSintomaEnum.Subjetivo;

        // ===== CAMPOS EXISTENTES =====
        public DateTime fechaEliminado { get; set; }
        public DateTime fechaModificado { get; set; }
        public DateTime fechaCreado { get; set; }
        public bool Eliminado { get; set; }

        // ===== NAVIGATION PROPERTIES =====
        public virtual ICollection<sintomasUsuario> SintomasUsuario { get; set; }
        public virtual ICollection<SintomasNotas> Notas { get; set; }
    }
}