using System.Collections.Generic;
using eiibd26.Services.Validacion;

namespace eiibd26.Pages.Platillos
{
    /// <summary>
    /// VM del bloque de validación inline de UNA nota clínica (sello + tarjeta del médico).
    /// Tipo COMPARTIDO de verdad (archivo .cs propio, no dentro de un .cshtml): lo expone el
    /// PageModel <see cref="IngredienteModel"/>, lo instancia la vista Ingrediente.cshtml y lo
    /// consume como @model el partial _NotaValidacion.cshtml. Los tres apuntan a este mismo tipo.
    /// </summary>
    public class NotaValidacionVm
    {
        /// <summary>= PlatNotaClinica.Id (ContenidoId de la validación).</summary>
        public int NotaId { get; set; }

        /// <summary>"queso" / "el grupo lácteos" — para desambiguar cuál nota se valida.</summary>
        public string DestinoLabel { get; set; } = "";

        /// <summary>Slug del ingrediente (página actual, para el redirect del POST).</summary>
        public string Slug { get; set; } = "";

        public bool CanValidate { get; set; }

        public ValidacionExistenteDto? MiValidacion { get; set; }

        public List<ValidacionPublicaDto> Validadores { get; set; } = new();
    }
}
