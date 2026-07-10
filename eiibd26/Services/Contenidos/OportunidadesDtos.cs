using System.Collections.Generic;
using eiibd26.Models.Contenidos;

namespace eiibd26.Services.Contenidos
{
    /// <summary>Una fila del backlog de oportunidades (un tema externo).</summary>
    public sealed class OportunidadRowDto
    {
        /// <summary>ScrapedPage.ScrapedPageId — RefId del estado (Tipo="Externo").</summary>
        public int ScrapedPageId { get; init; }

        /// <summary>Título legible del tema externo (derivado del slug de la URL).</summary>
        public string Titulo { get; init; } = "";

        /// <summary>Fuente que lo cubre (Educa / funeiico / MyCrohns…).</summary>
        public string Fuente { get; init; } = "";

        public string Url { get; init; } = "";

        /// <summary>"Lo más cercano que tenemos": solo en Ampliar. Null = no tenemos nada.</summary>
        public string? PropioCercanoTitulo { get; init; }
        public int? PropioCercanoId { get; init; }

        /// <summary>
        /// % de similitud (coseno de embeddings redondeado, <c>Round(MejorScore*100)</c>).
        /// Solo se llena en "Ampliar"; en "Escribir nuevo" es null (no hay match).
        /// </summary>
        public int? PorcentajeSimilitud { get; init; }

        /// <summary>Estado del backlog editable por el editor.</summary>
        public EstadoBacklog EstadoBacklog { get; init; }
    }

    /// <summary>Modelo del partial de tabla de oportunidades (reutilizado por las dos lentes).</summary>
    public sealed class OportunidadesTablaVm
    {
        public IReadOnlyList<OportunidadRowDto> Filas { get; init; } = new List<OportunidadRowDto>();

        /// <summary>Muestra la columna "lo más cercano que tenemos" (solo en Ampliar).</summary>
        public bool MostrarCercano { get; init; }
    }

    /// <summary>Payload completo de la vista de oportunidades (F1: dos lentes de cobertura).</summary>
    public sealed class OportunidadesVistaDto
    {
        public int TotalSinCubrir { get; init; }
        public int TotalAmpliar { get; init; }

        public IReadOnlyList<OportunidadRowDto> SinCubrir { get; init; } = new List<OportunidadRowDto>();
        public IReadOnlyList<OportunidadRowDto> Ampliar { get; init; } = new List<OportunidadRowDto>();

        /// <summary>Nombres de fuente presentes en los resultados — para el filtro por fuente.</summary>
        public IReadOnlyList<string> Fuentes { get; init; } = new List<string>();

        /// <summary>Motor no disponible / sin similitud calculada.</summary>
        public bool Disponible { get; init; } = true;
        public string? Aviso { get; init; }
    }
}
