using System.Collections.Generic;

namespace eiibd26.Services.Platillos
{
    /// <summary>Estado de la nota de un destino, para la columna de la lista admin.</summary>
    public enum PlatNotaEstado
    {
        /// <summary>No hay nota, o la que hay no tiene ninguna sección con contenido.</summary>
        SinNota = 0,
        /// <summary>Hay nota con contenido pero NO publicada: el paciente no la ve.</summary>
        Borrador = 1,
        /// <summary>Publicada + activa + con contenido: el paciente la ve (pasa el candado).</summary>
        Publicada = 2
    }

    /// <summary>Una sección editable de la nota (título opcional + contenido; viñeta por línea "- ").</summary>
    public class PlatNotaSeccionInput
    {
        public string? Titulo { get; set; }
        public string? Contenido { get; set; }
    }

    /// <summary>Una referencia bibliográfica editable (título obligatorio, URL opcional).</summary>
    public class PlatNotaReferenciaInput
    {
        public string? Titulo { get; set; }
        public string? Url { get; set; }
    }

    /// <summary>Todo lo que el panel lateral necesita para editar la nota de un destino.</summary>
    public class PlatNotaEditVm
    {
        public string TipoDestino { get; set; } = "";      // 'Grupo' | 'Ingrediente'
        public int DestinoId { get; set; }
        public string DestinoNombre { get; set; } = "";
        public int? NotaId { get; set; }                    // null si aún no existe fila
        public string Titulo { get; set; } = "";
        public bool Publicado { get; set; }
        public PlatNotaEstado Estado { get; set; }
        /// <summary>true si hay ≥1 sección con contenido → se puede publicar (mismo guard que el candado).</summary>
        public bool PuedePublicar { get; set; }
        public List<PlatNotaSeccionInput> Secciones { get; set; } = new();
        public List<PlatNotaReferenciaInput> Referencias { get; set; } = new();
    }
}
