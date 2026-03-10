namespace eiibd26.Services.Glossary.DTOs
{
    /// <summary>
    /// DTO para mostrar artículos CMS relacionados con un término
    /// </summary>
    public class RelatedContentDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Resumen { get; set; }
        public string? ImagenDestacada { get; set; }
        public DateTime? FechaPublicacion { get; set; }
    }
}
