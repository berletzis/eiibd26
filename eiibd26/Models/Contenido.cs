using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Representa la tabla dbo.contenidos
    /// </summary>

    public class Contenido
    {
        [Key]
        public int Id { get; set; }
        public int? IdTipo { get; set; }
        public string? ContenidoTitulo { get; set; }
        public string? ContenidoTextoC { get; set; }
        public string? ContenidoTextoL { get; set; }
        public string? ContenidoTituloSlug { get; set; }
        public string? URLImagenPrincipal { get; set; }
        public int? EstadoPublicacion { get; set; }
        public DateTime? ContenidoFechaInicio { get; set; }
        public DateTime? ContenidoFechaFin { get; set; }
        public Guid IdAutor { get; set; }
        public string? Autor { get; set; }
        public int? IdEmpresa { get; set; }
        public string? PaisClave { get; set; }
        public Guid? UsuarioModificacion { get; set; }
        public Guid UsuarioCreacion { get; set; }
        public DateTime? FechaModificado { get; set; }
        public DateTime FechaCreado { get; set; }
        public bool Eliminado { get; set; }
        public Guid? IdUser { get; set; }

        /// <summary>
        /// Firma de cobertura EII: JSON disperso { v, totalTokens, counts{term→conteo} }.
        /// Calculada por FirmaService sobre el vocabulario EII (glosario, relación Directa).
        /// NULL = aún no firmado. Columna agregada por SQL directo (sin migración EF).
        /// </summary>
        public string? Firma { get; set; }

        /// <summary>Momento del último cálculo de <see cref="Firma"/>. NULL = nunca firmado.</summary>
        public DateTime? FirmaCalculadaEn { get; set; }

        /// <summary>
        /// Embedding semántico denso (Voyage): JSON de un array de floats (p. ej. 1024 dims de
        /// voyage-4-large). Complementa a <see cref="Firma"/> (conteo de vocabulario): el embedding
        /// captura el significado para similitud fina "más como este". NULL = aún no embebido.
        /// "[]" = sin texto embebible (páginas institucionales). Columna por SQL directo (sin migración).
        /// </summary>
        public string? Embedding { get; set; }

        /// <summary>Modelo que generó el <see cref="Embedding"/> (p. ej. "voyage-4-large"), para detectar vectores obsoletos.</summary>
        public string? EmbeddingModelo { get; set; }

        /// <summary>Momento del último cálculo de <see cref="Embedding"/>. NULL = nunca embebido.</summary>
        public DateTime? EmbeddingCalculadoEn { get; set; }
        /* Navigation properties (optional — enable if the related entity classes exist) */
        [ForeignKey("IdAutor")]
        public virtual Perfil AutorPerfil { get; set; }

        [ForeignKey("IdUser")]
        public virtual ApplicationUser Usuario { get; set; }

        public virtual ICollection<ContenidoRespuesta> Respuestas { get; set; } = new List<ContenidoRespuesta>();
        public virtual ICollection<ContenidoCategoriaRelacion> CategoriasRelacion { get; set; } = new List<ContenidoCategoriaRelacion>();
        public virtual ICollection<ContenidoRelacionado> ContenidosRelacionados { get; set; } = new List<ContenidoRelacionado>();
        public virtual ICollection<ContenidoCalificacionArticuloPregunta> CalificacionesArticulosPreguntas { get; set; } = new List<ContenidoCalificacionArticuloPregunta>();
        public virtual ICollection<ContenidoPreguntaRelacion> PreguntasRelacion { get; set; } = new List<ContenidoPreguntaRelacion>();
        public virtual ICollection<ContenidoRespuestaRelacion> RespuestasRelacion { get; set; } = new List<ContenidoRespuestaRelacion>();
    }


}