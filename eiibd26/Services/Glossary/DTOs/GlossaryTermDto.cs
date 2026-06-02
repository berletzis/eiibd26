using eiibd26.Models.Glossary;
using eiibd26.Services.Validacion;

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
        /// Nivel de relación con la EII (confirmado por admin, o sugerido por IA si no hay confirmado)
        /// </summary>
        public MedicalRelationType? NivelRelacion { get; set; }

        /// <summary>
        /// Primera letra para agrupar alfabéticamente
        /// </summary>
        public char LetraInicial => string.IsNullOrEmpty(Nombre) ? '#' : char.ToUpper(Nombre[0]);

        public List<ValidacionPublicaDto> Validadores { get; set; } = new();
    }
}
