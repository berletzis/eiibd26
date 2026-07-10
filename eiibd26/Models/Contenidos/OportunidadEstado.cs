using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Contenidos
{
    /// <summary>Estado del backlog editorial de una oportunidad de contenido.</summary>
    public enum EstadoBacklog : byte
    {
        Nuevo = 0,
        Planificado = 1,
        EnProgreso = 2,
        Publicado = 3,
        Descartado = 4
    }

    /// <summary>
    /// Estado editable por ítem de la vista "Oportunidades de contenido" (F1).
    /// Tabla propia del Web (dbo.OportunidadEstado), creada por SQL directo.
    /// El cálculo de cobertura NO vive aquí — esta tabla solo guarda la decisión del editor.
    /// <para>Tipo = "Externo" → RefId es ScrapedPage.ScrapedPageId (lente cobertura).</para>
    /// <para>Tipo = "Propio"  → RefId es contenidos.Id (lente Mejorar/GRIS, F2).</para>
    /// </summary>
    [Table("OportunidadEstado")]
    public class OportunidadEstado
    {
        public const string TipoExterno = "Externo";
        public const string TipoPropio = "Propio";

        [Key]
        public int Id { get; set; }

        [MaxLength(10)]
        public string Tipo { get; set; } = TipoExterno;

        public int RefId { get; set; }

        /// <summary>Enum <see cref="EstadoBacklog"/> como byte.</summary>
        public byte Estado { get; set; }

        [MaxLength(450)]
        public string? EditorUserId { get; set; }

        public DateTime FechaActualizada { get; set; }
    }
}
