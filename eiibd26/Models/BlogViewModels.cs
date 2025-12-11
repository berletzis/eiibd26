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
        public string Category { get; set; } = "Business";
    }

    public class BlogListViewModel
    {
        public List<BlogItemVm> Items { get; set; } = new List<BlogItemVm>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}