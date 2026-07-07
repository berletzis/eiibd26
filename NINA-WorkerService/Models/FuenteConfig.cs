namespace NINA_WorkerService.Models;

/// <summary>
/// Modo de indexado de una fuente. Solo IndiceMetadatos está implementado
/// (índice tipo Google: metadatos + link, nunca el cuerpo del artículo).
/// ContenidoCompleto queda reservado para el futuro; NO está implementado.
/// </summary>
public enum ModoIndexado
{
    IndiceMetadatos,
    ContenidoCompleto // reservado — no implementado
}

/// <summary>
/// Configuración de una fuente a indexar, mapeada desde fuentes.json (System.Text.Json,
/// case-insensitive). El JSON es la fuente de verdad de qué sitios se indexan y con qué
/// metadatos; se edita a mano y viaja junto al ejecutable.
/// </summary>
public sealed class FuenteConfig
{
    public string Nombre { get; set; } = string.Empty;
    public string? SitioWebNombre { get; set; }
    public string? UrlPublica { get; set; }
    public string UrlInicial { get; set; } = string.Empty;
    public List<string> HostPermitidos { get; set; } = new();
    public string? Idioma { get; set; }
    public string? Pais { get; set; }
    public string? Categoria { get; set; }
    public ModoIndexado Modo { get; set; } = ModoIndexado.IndiceMetadatos;
    public int MaxDepth { get; set; } = 10;
    public int MaxPages { get; set; } = 3000;
    public bool Activo { get; set; }
}
