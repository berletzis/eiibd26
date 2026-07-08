using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Cobertura
{
    /// <summary>Tipo de par de similitud.</summary>
    public static class TipoParSimilitud
    {
        public const byte PropioPropio = 1;
        public const byte PropioExterno = 2;
    }

    /// <summary>
    /// Motor de Cobertura — Fase 3. Un par de artículos y su similitud de coseno.
    /// Tabla propia del Web (dbo.CoberturaSimilitud), creada por SQL directo.
    /// Sin FKs: BId puede ser un propio (contenidos.Id) o un externo (ScrapedPage.ScrapedPageId)
    /// según <see cref="TipoPar"/>.
    /// </summary>
    public class CoberturaSimilitud
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Siempre un artículo PROPIO (contenidos.Id).</summary>
        public int AId { get; set; }

        /// <summary>Propio (contenidos.Id) o externo (ScrapedPage.ScrapedPageId), según TipoPar.</summary>
        public int BId { get; set; }

        /// <summary>1 = propio-propio, 2 = propio-externo (ver <see cref="TipoParSimilitud"/>).</summary>
        public byte TipoPar { get; set; }

        /// <summary>Similitud de coseno 0–1.</summary>
        public decimal Score { get; set; }

        /// <summary>Timestamp de la firma de A al calcular (para el incremental).</summary>
        public DateTime? AFirmaEn { get; set; }

        /// <summary>Timestamp de la firma de B al calcular (para el incremental).</summary>
        public DateTime? BFirmaEn { get; set; }

        public DateTime CalculatedAt { get; set; }
    }
}
