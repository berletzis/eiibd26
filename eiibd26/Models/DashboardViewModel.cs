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
        public int DiagnosisUpdatesCount { get; set; } = 0;

        /// <summary>Estadísticas del período (últimos 7 días). Puede ser null si no hay datos.</summary>
        public HealthStatsDto? HealthStats { get; set; }
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

    public class SymptomTopItem
    {
        public int SintomaUsuarioId { get; set; }
        public string Nombre { get; set; }
        public int Interacciones { get; set; }
        public List<string> Condiciones { get; set; } = new List<string>();
        public Dictionary<string, string> SeguimientoPorDia { get; set; } = new Dictionary<string, string>();
    }

    public class QuestionItem
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; }
        public int AnswersCount { get; set; }
        public int Votes { get; set; }
        public string? Slug { get; set; }
    }

    public class AnswerItem
    {
        public Guid Id { get; set; }
        public string Cuerpo { get; set; }
        public int Votes { get; set; }
        public Guid? PreguntaId { get; set; }
        public string? Slug { get; set; }
    }
}