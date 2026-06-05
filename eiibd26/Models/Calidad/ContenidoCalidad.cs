using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Calidad
{
    [Table("ContenidoCalidad")]
    public class ContenidoCalidad
    {
        [Key]
        public int Id { get; set; }

        public int ContenidoId { get; set; }

        /// <summary>Enum NivelSemaforo como byte: 0=Critico, 1=Mejorable, 2=Ok.</summary>
        public byte NivelSemaforo { get; set; }

        /// <summary>JSON: [{Codigo, Descripcion, Gravedad}, ...]</summary>
        public string? Senales { get; set; }

        /// <summary>JSON: [42, 87, ...] — IDs de contenidos similares detectados.</summary>
        public string? DuplicadoDeIds { get; set; }

        public DateTime FechaAnalisis { get; set; }

        // GRIS — evaluación editorial con IA (rúbrica 7 aspectos)
        public bool GrisEvaluado { get; set; }

        /// <summary>Puntaje global editorial 0-100.</summary>
        public byte? GrisPuntajeGlobal { get; set; }

        /// <summary>JSON: [{nombre, puntaje, observacion}, ...] — 7 aspectos.</summary>
        public string? GrisResultado { get; set; }

        /// <summary>JSON: ["sugerencia1", ...] — puede ser texto crudo si el parse falló.</summary>
        public string? GrisSugerencias { get; set; }

        /// <summary>JSON: [{categoriaId, nombre, razon}] — categorías donde encajaría el artículo.</summary>
        public string? GrisCategoriasSugeridas { get; set; }

        /// <summary>JSON: [{categoriaId, nombre, razon}] — categorías actuales que no corresponden al contenido.</summary>
        public string? GrisCategoriasAlerta { get; set; }

        public DateTime? GrisFechaEvaluacion { get; set; }
    }
}
