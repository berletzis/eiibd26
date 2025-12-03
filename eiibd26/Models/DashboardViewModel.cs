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
    }

    public class QuestionItem
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; }
        public int AnswersCount { get; set; }
        public int Votes { get; set; }
    }

    public class AnswerItem
    {
        public Guid Id { get; set; }
        public string Cuerpo { get; set; }
        public int Votes { get; set; }
        public Guid? PreguntaId { get; set; }
    }
}