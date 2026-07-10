using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Cobertura
{
    /// <summary>
    /// Vista READ-ONLY de dbo.Article (tabla del Worker) para leer el título real del externo.
    /// El Worker extrae el título (title → og:title → h1) en ExtractMetadata y lo persiste en
    /// <see cref="NormalizedTitle"/> — pese al nombre, es el título crudo (no lowercaseado).
    /// El Web NUNCA escribe esta tabla. Solo las columnas necesarias para el display.
    /// </summary>
    public class ArticleRef
    {
        [Key]
        public int ArticleId { get; set; }

        /// <summary>FK al ancla de URL (ScrapedPage). Llave del join con la fila de cobertura.</summary>
        public int? ScrapedPageId { get; set; }

        /// <summary>Título real del externo (crudo, de ExtractMetadata). Usado para el display.</summary>
        public string NormalizedTitle { get; set; } = "";
    }
}
