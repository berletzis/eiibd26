using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Cobertura
{
    /// <summary>
    /// Motor de Cobertura — Fase 5. Un par de artículos y su similitud de COSENO sobre
    /// embeddings densos (Voyage). Tabla propia del Web (dbo.CoberturaSimilitudEmbedding),
    /// PARALELA a <see cref="CoberturaSimilitud"/> (firma) para no mezclar métricas durante el
    /// standby. Reusa <see cref="TipoParSimilitud"/> (1=propio-propio, 2=propio-externo).
    /// Sin FKs: BId puede ser un propio (contenidos.Id) o un externo (ScrapedPage.ScrapedPageId)
    /// según <see cref="TipoPar"/>.
    /// </summary>
    public class CoberturaSimilitudEmbedding
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Siempre un artículo PROPIO (contenidos.Id).</summary>
        public int AId { get; set; }

        /// <summary>Propio (contenidos.Id) o externo (ScrapedPage.ScrapedPageId), según TipoPar.</summary>
        public int BId { get; set; }

        /// <summary>1 = propio-propio, 2 = propio-externo (ver <see cref="TipoParSimilitud"/>).</summary>
        public byte TipoPar { get; set; }

        /// <summary>Similitud de coseno de embeddings (−1..1; en la práctica 0..1).</summary>
        public decimal Score { get; set; }

        /// <summary>Timestamp del embedding de A al calcular (para el incremental).</summary>
        public DateTime? AEmbEn { get; set; }

        /// <summary>Timestamp del embedding de B al calcular (para el incremental).</summary>
        public DateTime? BEmbEn { get; set; }

        public DateTime CalculatedAt { get; set; }
    }
}
