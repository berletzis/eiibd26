using System.Collections.Generic;

namespace eiibd26.Models
{
    // View model to represent paged contents for a given category.
    // Location requested: eiibd26/Models/ContenidoporCategoria.cs
    public class ContenidoporCategoria
    {
        public int CategorySeq { get; set; }
        public string CategoryName { get; set; } = "";

        // Paging (defaults similar to the rest of the project)
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 7;

        // Results
        public int TotalCount { get; set; } = 0;
        public List<BlogItemVm> Items { get; set; } = new List<BlogItemVm>();
    }
}