using eiibd26.Models.Glossary;

namespace eiibd26.Services.Glossary.DTOs
{
    /// <summary>
    /// DTO para listar términos del glosario (índice A-Z)
    /// </summary>
    public class GlossaryTermDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Slug { get; set; } = "";
        public GlossaryTermType TipoTermino { get; set; }
        
        /// <summary>
        /// Primera letra para agrupar alfabéticamente
        /// </summary>
        public char LetraInicial => string.IsNullOrEmpty(Nombre) ? '#' : char.ToUpper(Nombre[0]);
    }
}
