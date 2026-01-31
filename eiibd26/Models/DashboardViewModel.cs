using System;
using System.Collections.Generic;

namespace eiibd26.Models
{
    public class DashboardViewModel
    {
        public List<MoodPoint> Moods { get; set; } = new List<MoodPoint>();
        public List<RelationItem> MoodRelations { get; set; } = new List<RelationItem>();
        public List<SymptomTopItem> TopSintomas { get; set; } = new List<SymptomTopItem>();
        public List<QuestionItem> Preguntas { get; set; } = new List<QuestionItem>();
        public List<AnswerItem> Respuestas { get; set; } = new List<AnswerItem>();

        // New: properties for the notifications card
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool HasAnyCondition { get; set; }
        public bool HasMoodToday { get; set; }

        // Counts / metadata for notifications
        public int NewAnswersCount { get; set; } = 0;
        public int ScheduledItemsCount { get; set; } = 0; // placeholder for future scheduled tasks
    }

    public class MoodPoint
    {
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } // MuyBien, Bien, Neutral, Mal, MuyMal
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