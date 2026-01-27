using System;
using System.Collections.Generic;

namespace eiibd26.Models
{
    public enum RelatedType
    {
        Contenido = 1,
        Pregunta = 2
    }

    public class ContenidoDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Excerpt { get; set; } = "";
        public string ContentHtml { get; set; } = "";
        public string ImageUrl { get; set; }
        public string Author { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int ReadMinutes { get; set; }

        public List<string> Categories { get; set; } = new List<string>();

        // manual relations (contenidos añadidos manualmente)
        public List<RelatedContenidoVm> ManualRelated { get; set; } = new List<RelatedContenidoVm>();

        // automatic related by categories
        public List<RelatedContenidoVm> RelatedByCategories { get; set; } = new List<RelatedContenidoVm>();

        // final combined: manual (up to N) then automatic (up to N), no duplicates
        public List<RelatedContenidoVm> AllRelated { get; set; } = new List<RelatedContenidoVm>();

        // manual domain relations
        public List<string> ManualCondiciones { get; set; } = new List<string>();
        public List<string> ManualSintomas { get; set; } = new List<string>();
        public List<string> ManualTratamientos { get; set; } = new List<string>();

        // Manual related preguntas (questions) — added to support manual question relations
        public List<RelatedPreguntaVm> ManualPreguntas { get; set; } = new List<RelatedPreguntaVm>();

        public string Slug { get; set; } = "";

        // Nested VM for question items
        public class RelatedPreguntaVm
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = "";
            public string Slug { get; set; } = "";  
            public DateTime CreatedAt { get; set; }
            public string Note { get; set; } = "";
        }
    }

    public class RelatedContenidoVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Category { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Note { get; set; } = ""; // optional: description from manual relation
        public bool IsManual { get; set; } = false;
        public RelatedType Type { get; set; } = RelatedType.Contenido;
    }
}