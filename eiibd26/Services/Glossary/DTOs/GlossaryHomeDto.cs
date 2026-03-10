namespace eiibd26.Services.Glossary.DTOs
{
    /// <summary>
    /// DTO para la página de inicio del glosario
    /// </summary>
    public class GlossaryHomeDto
    {
        /// <summary>
        /// Total de síntomas en el glosario
        /// </summary>
        public int TotalSintomas { get; set; }
        
        /// <summary>
        /// Total de tratamientos en el glosario
        /// </summary>
        public int TotalTratamientos { get; set; }
        
        /// <summary>
        /// Términos destacados o recientes (opcional)
        /// </summary>
        public List<GlossaryTermDto> TerminosDestacados { get; set; } = new();
    }
}
