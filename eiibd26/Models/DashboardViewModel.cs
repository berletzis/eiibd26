using System;
using System.Collections.Generic;
using eiibd26.DTOs.Analytics;

namespace eiibd26.Models
{
    public class DashboardViewModel
    {
        public List<MoodPoint> Moods { get; set; } = new List<MoodPoint>();
        public List<RelationItem> MoodRelations { get; set; } = new List<RelationItem>();
        public List<SymptomTopItem> TopSintomas { get; set; } = new List<SymptomTopItem>();
        public List<QuestionItem> Preguntas { get; set; } = new List<QuestionItem>();
        public List<AnswerItem> Respuestas { get; set; } = new List<AnswerItem>();

        // ✅ AGREGAR ESTAS PROPIEDADES:
        public List<RelationItem> UserConditions { get; set; } = new List<RelationItem>();
        public List<RelationItem> UserSymptoms { get; set; } = new List<RelationItem>();
        public List<RelationItem> UserTreatments { get; set; } = new List<RelationItem>();

        // Flags para notificaciones
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool HasAnyCondition { get; set; }
        public bool HasMoodToday { get; set; }
        public int NewAnswersCount { get; set; } = 0;
        public int ScheduledItemsCount { get; set; } = 0;
        // Indica si al menos una condición tiene fecha de diagnóstico igual a la fecha de registro
        // del usuario y por tanto debe actualizarse.
        public bool NeedsDiagnosisDateUpdate { get; set; } = false;
        public bool NeedsAvatar { get; set; } = false;
        public int DiagnosisUpdatesCount { get; set; } = 0;
        public bool HasRealName { get; set; }
        public int TotalLaboratorios { get; set; } = 0;
        public string UltimoLaboratorio { get; set; } = "";
        public List<LabResultDashItem> UltimosLaboratorios { get; set; } = new();

        /// <summary>Estadísticas del período (últimos 7 días). Puede ser null si no hay datos.</summary>
        public HealthStatsDto? HealthStats { get; set; }

        /// <summary>Insights clínicos del período (últimos 7 días). Puede ser null si no hay datos.</summary>
        public HealthInsightDto? HealthInsight { get; set; }

        /// <summary>Catálogo de frecuencias para los dropdowns de tracking.</summary>
        public List<FrecuenciaSintomaCatalog> FrecuenciasCatalog { get; set; } = new();
    }

    public class MoodPoint
    {
        public DateTime Fecha { get; set; }
        public int Estado { get; set; } // ✅ Cambiar de string a int
        public string Texto { get; set; }
        public string RelacionNombre { get; set; }
    }

    public class RelationItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
    }

    public class DiaTracking
    {
        public string Estado { get; set; } = "";
        public int? Dolor { get; set; }
        public bool? TieneSangrado { get; set; }
        public int? FrecuenciaId { get; set; }
    }

    public class SymptomTopItem
    {
        public int SintomaUsuarioId { get; set; }
        public string Nombre { get; set; }
        public int Interacciones { get; set; }
        public List<string> Condiciones { get; set; } = new List<string>();
        public Dictionary<string, DiaTracking> SeguimientoPorDia { get; set; } = new Dictionary<string, DiaTracking>();
        /// <summary>Promedio de dolor registrado en el período (0-10). Null si no hay registros con dolor.</summary>
        public double? PromedioDolor { get; set; }
        /// <summary>Indica si el último registro con sangrado informado fue positivo. Null si no hay datos.</summary>
        public bool? UltimoTieneSangrado { get; set; }
        /// <summary>Nombre de frecuencia del último registro que la informó. Null si ninguno la registró.</summary>
        public string? UltimaFrecuenciaNombre { get; set; }
        /// <summary>Tipo de síntoma del catálogo: 0=Subjetivo, 1=Medible GI, 2=Dolor.</summary>
        public int TipoSintoma { get; set; } = 0;
    }

    public class QuestionItem
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; }
        public int AnswersCount { get; set; }
        public int Votes { get; set; }
        public string? Slug { get; set; }
        /// <summary>Solo para ordenamiento con decay temporal. No se expone en la vista.</summary>
        public DateTimeOffset FechaCreacion { get; set; }
    }

    public class AnswerItem
    {
        public Guid Id { get; set; }
        public string Cuerpo { get; set; }
        public int Votes { get; set; }
        public Guid? PreguntaId { get; set; }
        public string? Slug { get; set; }
    }

    public class LabResultDashItem
    {
        public int Id { get; set; }
        public string Estudio { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string? Resultado { get; set; }
        public string? Unidad { get; set; }
        public DateTime? FechaResultado { get; set; }
        public string? Condicion { get; set; }
        public string? Sintoma { get; set; }
        public string? Tratamiento { get; set; }
    }
}