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
    }
}
