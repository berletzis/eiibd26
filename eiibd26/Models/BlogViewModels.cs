// eiibd26/Models/BlogViewModels.cs
using System;
using System.Collections.Generic;

namespace eiibd26.Models
{
    public class HeroViewModel
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string CallToAction { get; set; } = "";
    }

    public class BlogItemVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Excerpt { get; set; } = "";
        public string ImageUrl { get; set; }
        public string Author { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Slug { get; set; } = "";  // ← Asegurar que existe
        public string CategorySlug { get; set; } = "";  // ← AGREGAR
        public string Category { get; set; }
        // Related metadata (lazy-loaded via ContentMeta endpoint or prefilled)
        public List<string> Conditions { get; set; } = new List<string>();
        public List<string> Symptoms { get; set; } = new List<string>();
        public List<string> Treatments { get; set; } = new List<string>();

        // Number of distinct related questions for this content
        public int RelatedQuestionsCount { get; set; } = 0;
    }

    public class BlogListViewModel
    {
        public List<BlogItemVm> Items { get; set; } = new List<BlogItemVm>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}