using eiibd26.Models.Validacion;

namespace eiibd26.Services.Platillos
{
    /// <summary>
    /// Una nota clínica de alimentos sugerida a un profesional para validar, ya resuelta
    /// al punto donde REALMENTE se valida.
    ///
    /// Ojo con <see cref="SlugIngredienteParaValidar"/>: no existe ficha pública de grupo.
    /// Las notas de grupo (y las de precaución, que también son de grupo) se validan desde
    /// la ficha de cualquier ingrediente de ese grupo — ahí es donde Ingrediente.cshtml.cs
    /// carga GrupoNota/PrecaucionNota y postea la validación. Por eso una nota de grupo sin
    /// ingredientes activos NO se sugiere: sería un enlace muerto.
    /// </summary>
    public class PlatNotaParaValidarDto
    {
        /// <summary>Id de PlatNotaClinica — es el ContenidoId de la validación.</summary>
        public int NotaId { get; set; }

        public string Titulo { get; set; } = "";

        /// <summary>'Grupo' | 'Ingrediente'.</summary>
        public string TipoDestino { get; set; } = "";

        /// <summary>'Tolerancia' | 'Precaucion'.</summary>
        public string TipoNota { get; set; } = "";

        /// <summary>Nombre del grupo o del ingrediente al que pertenece la nota.</summary>
        public string DestinoNombre { get; set; } = "";

        /// <summary>Slug del ingrediente por el que se llega a validar esta nota.</summary>
        public string SlugIngredienteParaValidar { get; set; } = "";

        /// <summary>Nombre de ese ingrediente. Difiere de DestinoNombre en notas de grupo:
        /// sirve para explicar el salto ("se valida desde X").</summary>
        public string NombreIngredienteParaValidar { get; set; } = "";

        /// <summary>Validaciones aprobadas de otros profesionales. Menos = más lo necesita.</summary>
        public int TotalValidaciones { get; set; }

        /// <summary>Estado de la validación del profesional que consulta. Null = no la ha validado.</summary>
        public EstadoValidacion? MiEstado { get; set; }

        public string Url => $"/Platillos/Ingrediente/{SlugIngredienteParaValidar}";

        /// <summary>True cuando la nota es de grupo y por tanto el enlace lleva a otro nombre.</summary>
        public bool EsDeGrupo => TipoDestino == "Grupo";

        public bool EsPrecaucion => TipoNota == "Precaucion";

        public string TipoTexto => EsPrecaucion ? "Precaución"
                                 : EsDeGrupo    ? "Grupo"
                                 : "Ingrediente";
    }
}
