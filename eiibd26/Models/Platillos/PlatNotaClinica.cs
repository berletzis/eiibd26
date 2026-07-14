using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Nota clínica estructurada de un grupo o de un ingrediente ("¿Puedo comer lácteos?").
    /// Contenido MÉDICO. Candado por construcción: nace <c>Publicado = false</c> (borrador) y NINGUNA
    /// ruta debe mostrarla al paciente hasta que un ADMINISTRADOR la publique.
    ///
    /// Publicar (este flag) ≠ validación médica: la validación es una SEÑAL de confianza aparte
    /// (médicos que respaldan la nota) y NO controla la visibilidad. Aquí solo vive el interruptor
    /// de publicación.
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

        /// <summary>EL CANDADO. Solo un administrador lo levanta desde el CRUD admin (F2a). Default 0 en la BD.
        /// Al editar el contenido de una nota publicada, se vuelve a poner en 0 (regresa a borrador).</summary>
        public bool Publicado { get; set; }

        /// <summary>Qué administrador la publicó. Responsable de la decisión de mostrarla.</summary>
        public Guid? PublicadaPorUserId { get; set; }

        public DateTime? FechaPublicacion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; }

        public virtual ICollection<PlatNotaSeccion> Secciones { get; set; } = new List<PlatNotaSeccion>();
        public virtual ICollection<PlatNotaReferencia> Referencias { get; set; } = new List<PlatNotaReferencia>();
    }
}
