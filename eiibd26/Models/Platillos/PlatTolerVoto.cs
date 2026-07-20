using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>1 = Sí · 2 = A veces · 3 = No. Se persiste como TINYINT (HasConversion&lt;byte&gt;).</summary>
    public enum PlatToleraNivel : byte
    {
        Si = 1,
        AVeces = 2,
        No = 3
    }

    /// <summary>
    /// Voto de la encuesta de tolerancia (/tolero/{slug}) sobre UN ingrediente.
    /// Tabla PROPIA polimórfica sin FK física (mismo patrón que <see cref="PlatCalificacion"/>).
    /// Un voto por paciente (<see cref="UserId"/>) o por cookie anónima (<see cref="AnonId"/>) por
    /// ingrediente — UNIQUE filtrado; el upsert de la página permite CAMBIAR el voto.
    ///
    /// La encuesta permite anónimo A PROPÓSITO (alcance viral, no rating médico — a diferencia de
    /// M-4). Dedup por cookie + rate-limit por IP. Alimenta el modelo bayesiano futuro (#16):
    /// se guarda la condición del paciente desde ya aunque el MVP no segmente.
    /// </summary>
    public class PlatTolerVoto
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Id de PlatIngrediente. Referencia lógica (sin FK física, por aislamiento del módulo).</summary>
        public int IngredienteId { get; set; }

        /// <summary>Usuario de Identity si votó logueado; null si anónimo.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Cookie de dedup para votos anónimos; null si logueado.</summary>
        public Guid? AnonId { get; set; }

        public PlatToleraNivel Tolera { get; set; }

        /// <summary>
        /// Condición principal CRUDA del paciente al votar (id de <c>condiciones</c>). FUENTE DE VERDAD
        /// para #16: permite recalcular el tipo de EII contra el catálogo aunque se renombre una condición.
        /// </summary>
        public int? CondicionIdPrincipal { get; set; }

        /// <summary>
        /// 1 = CUCI · 2 = Crohn; null si anónimo/desconocido. Denormalización de conveniencia derivada
        /// de <see cref="CondicionIdPrincipal"/> — recomputable, NO es la fuente de verdad.
        /// </summary>
        public byte? TipoEII { get; set; }

        public DateTime FechaVoto { get; set; }
    }
}
