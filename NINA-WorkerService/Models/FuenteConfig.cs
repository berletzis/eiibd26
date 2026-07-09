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
    // Opcional: si tiene prefijos, solo se sigue/indexa URLs cuyo path empiece con alguno
    // (case-insensitive). Vacío/ausente = todo el host permitido.
    public List<string> RutasPermitidas { get; set; } = new();
    public string? Idioma { get; set; }
    public string? Pais { get; set; }
    public string? Categoria { get; set; }
    public ModoIndexado Modo { get; set; } = ModoIndexado.IndiceMetadatos;
    public int MaxDepth { get; set; } = 10;
    public int MaxPages { get; set; } = 3000;
    public bool Activo { get; set; }

    // Sitemap como fuente de URLs (v2). Si usarSitemap, la lista del sitemap reemplaza el BFS.
    public bool UsarSitemap { get; set; } = true;
    public string? SitemapUrl { get; set; }              // opcional; si no, se busca en robots.txt / rutas estándar
    public List<string> ExcluirSitemaps { get; set; } = new(); // sub-sitemaps a excluir por nombre (substring)

    // Allowlist de sub-sitemaps (modo IndiceMetadatos con sitemap-index): si tiene elementos,
    // SOLO se procesan los sub-sitemaps cuya URL contenga alguno (substring, case-insensitive).
    // Vacío/ausente = comportamiento actual (todos los sub-sitemaps salvo los de ExcluirSitemaps).
    public List<string> SitemapsIncluidos { get; set; } = new();

    // Denylist de patrones de URL propia de la fuente: substring / glob '*' / regex con prefijo 're:'.
    // Se evalúa (case-insensitive) contra el path+query. Se combina con exclusiones-globales.json.
    public List<string> ExclusionesUrl { get; set; } = new();
}
