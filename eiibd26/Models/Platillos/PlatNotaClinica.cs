using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Nota clínica estructurada de un grupo o de un ingrediente ("¿Puedo comer lácteos?").
    /// Contenido MÉDICO. Candado por construcción: nace <c>RevisadaPorMedico = false</c> y NINGUNA
    /// ruta debe mostrarla al paciente hasta que un médico la apruebe.
    ///
    /// La lectura para el paciente pasa SIEMPRE por <see cref="Services.Platillos.IPlatNotaClinicaService"/>
    /// (único punto que aplica el candado). No consultar esta tabla directo desde una vista/controlador
    /// para pintar contenido: eso reabre la rendija que este modelo existe para cerrar.
    /// </summary>
    public class PlatNotaClinica
    {
        [Key]
        public int Id { get; set; }

        /// <summary>'Grupo' | 'Ingrediente'. Relación polimórfica con DestinoId (sin FK física, por aislamiento).</summary>
        public string TipoDestino { get; set; } = "";

        /// <summary>Id de PlatGrupo o PlatIngrediente según TipoDestino.</summary>
        public int DestinoId { get; set; }

        public string Titulo { get; set; } = "";

        /// <summary>EL CANDADO. Solo un médico lo levanta desde el CRUD admin (F2). Default 0 en la BD.</summary>
        public bool RevisadaPorMedico { get; set; }

        /// <summary>Quién la revisó. Responsable de que el texto sea correcto.</summary>
        public Guid? RevisadaPorUserId { get; set; }

        public DateTime? FechaRevision { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; }

        public virtual ICollection<PlatNotaSeccion> Secciones { get; set; } = new List<PlatNotaSeccion>();
        public virtual ICollection<PlatNotaReferencia> Referencias { get; set; } = new List<PlatNotaReferencia>();
    }
}
