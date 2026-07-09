using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Cobertura
{
    /// <summary>
    /// Vista READ-ONLY de dbo.SourceSite (tabla del Worker) para mostrar el nombre del sitio
    /// externo en las vistas de cobertura. El Web NUNCA escribe esta tabla.
    /// </summary>
    public class SourceSiteRef
    {
        [Key]
        public int SourceSiteId { get; set; }
        public string Name { get; set; } = "";
        public string? UrlPublica { get; set; }
    }
}
