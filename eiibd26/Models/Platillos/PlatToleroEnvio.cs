using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Control de envío de la encuesta de tolerancia (/tolero/{slug}) por ingrediente:
    /// "¿ya mandé la liga de este alimento?". UNA fila por ingrediente — solo la ÚLTIMA vez que se
    /// marcó como enviada, sin historial (varios envíos, canal, nota = versión futura).
    ///
    /// Tabla PROPIA sin FK física (mismo patrón que <see cref="PlatTolerVoto"/> / PlatCalificacion):
    /// es estado de CAMPAÑA, deliberadamente separado del catálogo (PlatIngrediente) para no
    /// mezclar "qué es el alimento" con "qué hice yo con él".
    ///
    /// No participa en ningún cálculo: el bayesiano (#16) no lee esta tabla.
    /// </summary>
    public class PlatToleroEnvio
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Id de PlatIngrediente. Referencia lógica (sin FK física). UNIQUE: una fila por ingrediente.</summary>
        public int IngredienteId { get; set; }

        /// <summary>
        /// Última vez que el admin marcó la liga como enviada (UTC). <c>null</c> = pendiente.
        /// Es un dato MANUAL: lo pone el admin cuando considera que el envío cuenta, no lo infiere
        /// el sistema (aquí no se manda ningún correo).
        /// </summary>
        public DateTime? EnviadaEn { get; set; }

        /// <summary>Admin que la marcó, para saber a quién preguntarle. Null si se deshizo.</summary>
        public Guid? MarcadaPorUserId { get; set; }
    }
}
