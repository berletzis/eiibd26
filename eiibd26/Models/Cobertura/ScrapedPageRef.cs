using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Cobertura
{
    /// <summary>
    /// Vista READ-ONLY de dbo.ScrapedPage (tabla del Worker) para que el Web lea la firma
    /// de los externos. El Web NUNCA escribe esta tabla — es dueña del Worker.
    /// Solo las columnas necesarias para la similitud.
    /// </summary>
    public class ScrapedPageRef
    {
        [Key]
        public int ScrapedPageId { get; set; }
        public int SourceSiteId { get; set; }
        public string Url { get; set; } = "";
        public string Language { get; set; } = "es";
        public string? Firma { get; set; }
        public DateTime? FirmaCalculadaEn { get; set; }
    }
}
