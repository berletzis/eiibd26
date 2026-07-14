using System.Collections.Generic;

namespace eiibd26.Services.Platillos
{
    /// <summary>Una nota clínica YA filtrada por el candado (revisada + activa + con contenido real).
    /// Si el servicio te devolvió esto, es seguro pintarlo al paciente.</summary>
    public class PlatNotaVisibleDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public List<PlatNotaSeccionDto> Secciones { get; set; } = new();
        public List<PlatNotaReferenciaDto> Referencias { get; set; } = new();
    }

    /// <summary>Sección con su contenido ya parseado en bloques (párrafos y listas).</summary>
    public class PlatNotaSeccionDto
    {
        public string? Titulo { get; set; }
        public List<PlatNotaBloqueDto> Bloques { get; set; } = new();
    }

    /// <summary>Un bloque de contenido: un párrafo (una línea) o una lista de viñetas.</summary>
    public class PlatNotaBloqueDto
    {
        /// <summary>true → &lt;ul&gt; de viñetas; false → &lt;p&gt; de una línea.</summary>
        public bool EsLista { get; set; }
        public List<string> Lineas { get; set; } = new();
    }

    public class PlatNotaReferenciaDto
    {
        public string Titulo { get; set; } = "";
        public string? Url { get; set; }
    }
}
