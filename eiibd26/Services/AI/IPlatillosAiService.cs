using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Resultado estructurado de una nota clínica generada por IA (grupo o ingrediente).
    /// Cada campo mapea a una sección de la nota; <see cref="Fuentes"/> ya viene FILTRADO contra
    /// la lista blanca (candado backend, no solo prompt). El controller nunca guarda esto: lo
    /// devuelve al editor para que el humano revise y guarde.
    /// </summary>
    public class NotaClinicaGeneradaDto
    {
        public string Titulo { get; set; } = "";
        public string QueEs { get; set; } = "";
        public string QueSuelePasar { get; set; } = "";
        public string Importante { get; set; } = "";
        /// <summary>Solo fuentes que pasaron el filtro de lista blanca. Vacío = sin referencias.</summary>
        public List<string> Fuentes { get; set; } = new();
        /// <summary>true si el tema es sensible y no hubo fuente aplicable → el admin la revisa antes de publicar.</summary>
        public bool RevisionPrioritaria { get; set; }
    }

    /// <summary>
    /// Generación de contenido del módulo Platillos con IA. Hermano de
    /// <see cref="ISintomasTratamientosAiService"/>: mismo cliente Anthropic y mismo estilo de prompt,
    /// pero con DOS niveles de riesgo que NUNCA se mezclan:
    ///   • CLÍNICO (notas de grupo/ingrediente, NotasEII) — candados: lista blanca de fuentes,
    ///     nunca prescribir, siempre borrador.
    ///   • TAXONÓMICO (descripción de atributo / categoría) — ligero: sin fuentes, sin afirmaciones clínicas.
    /// El servicio SOLO genera texto; publicar y guardar es siempre acto humano.
    /// </summary>
    public interface IPlatillosAiService
    {
        /// <summary>Nota clínica estructurada de un GRUPO de alimentos.</summary>
        Task<NotaClinicaGeneradaDto> GenerarNotaClinicaGrupoAsync(
            string nombreGrupo, IEnumerable<string> ingredientesDelGrupo,
            CancellationToken cancellationToken = default);

        /// <summary>Nota clínica estructurada de un INGREDIENTE (solo lo específico que no cubre su grupo).</summary>
        Task<NotaClinicaGeneradaDto> GenerarNotaClinicaIngredienteAsync(
            string nombreIngrediente, string? nombreGrupo, IEnumerable<string> atributos,
            CancellationToken cancellationToken = default);

        /// <summary>Anexo 5: AVISO DE PRECAUCIÓN de seguridad alimentaria para un GRUPO de riesgo.
        /// Distinto de la nota de tolerancia: eje de infección, condicional a inmunosupresión y atado a
        /// la preparación (crudo/poco cocido). Misma salida estructurada; se pinta como callout ámbar.</summary>
        Task<NotaClinicaGeneradaDto> GenerarNotaPrecaucionGrupoAsync(
            string nombreGrupo, string? riesgoTipo, IEnumerable<string> ingredientesDelGrupo,
            CancellationToken cancellationToken = default);

        /// <summary>Texto CORTO para NotasEII: una frase ≤120 caracteres, observacional, sin fuentes.</summary>
        Task<string> GenerarNotasEiiAsync(
            string tipo, string nombre, CancellationToken cancellationToken = default);

        /// <summary>Descripción TAXONÓMICA de un atributo (1-2 frases), respetando su Ámbito.</summary>
        Task<string> GenerarDescripcionAtributoAsync(
            string nombreAtributo, string ambito, CancellationToken cancellationToken = default);

        /// <summary>Descripción TAXONÓMICA de una categoría de platillo (1-2 frases).</summary>
        Task<string> GenerarDescripcionCategoriaAsync(
            string nombreCategoria, CancellationToken cancellationToken = default);
    }
}
