using eiibd_IMX.Data;
using eiibd_IMX.Models.Configuracion;
using eiibd_IMX.Models.ViewModels;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

namespace eiibd_IMX.Services;

public sealed class ContenidoService : IContenidoService
{
    private readonly ImxDbContext _db;
    private readonly HtmlSanitizer _sanitizer;
    private readonly IConfiguration _cfg;
    private readonly bool _respetarVigencias;

    public ContenidoService(ImxDbContext db, HtmlSanitizer sanitizer, IConfiguration cfg)
    {
        _db = db;
        _sanitizer = sanitizer;
        _cfg = cfg;
        _respetarVigencias = cfg.GetValue("Publicacion:RespetarVigencias", false);
    }

    // ── Criterio unico de "publicado" ────────────────────────────────────────
    // El admin de EIIBD solo escribe dos estados: 0 = Borrador, 1 = Publicado.
    // Se exige == 1 y no != 0: hay filas historicas con otros valores y un
    // "distinto de cero" las publicaria sin que nadie lo haya decidido.
    // Esta expresion es la UNICA fuente de verdad; todos los listados y el
    // detalle pasan por ella, para que no puedan divergir.
    private static readonly System.Linq.Expressions.Expression<
        Func<Models.ReadOnly.ContenidoRow, bool>> EsPublicado =
            c => !c.Eliminado
              && c.EstadoPublicacion == 1
              && c.ContenidoTituloSlug != null
              && c.ContenidoTituloSlug != "";

    // ── Vigencia ─────────────────────────────────────────────────────────────
    // contenidos.ContenidoFechaInicio / FechaFin todavia no se usan en EIIBD,
    // pero se van a usar. La logica queda escrita y apagada tras la bandera
    // Publicacion:RespetarVigencias, para que activarla sea cambiar un bool.
    // Null = sin restriccion por ese extremo: una fila sin fechas se comporta
    // exactamente igual con la bandera encendida o apagada.
    private static readonly System.Linq.Expressions.Expression<
        Func<Models.ReadOnly.ContenidoRow, bool>> EstaVigente =
            c => (c.ContenidoFechaInicio == null || c.ContenidoFechaInicio <= DateTime.Now)
              && (c.ContenidoFechaFin    == null || c.ContenidoFechaFin    >= DateTime.Now);

    private sealed record CategoriaBase(int Sequence, string Nombre, string Descripcion);

    private async Task<CategoriaBase?> BuscarCategoriaAsync(string slug, CancellationToken ct)
        => await _db.Categorias.AsNoTracking()
            .Where(c => !c.Borrado && c.CategoriaSlug == slug)
            .Select(c => new CategoriaBase(c.Sequence, c.Nombre ?? slug, c.Descripcion ?? ""))
            .FirstOrDefaultAsync(ct);

    /// Ids de la categoria y de sus hijas: las dos categorias de la home son
    /// padres, y el contenido cuelga de las hijas.
    private async Task<List<int>> IdsDeArbolAsync(int raiz, CancellationToken ct)
    {
        var ids = await _db.Categorias.AsNoTracking()
            .Where(c => !c.Borrado && c.CategoriaPadre == raiz)
            .Select(c => c.Sequence)
            .ToListAsync(ct);
        ids.Add(raiz);
        return ids;
    }

    /// Consulta base. Parte de contenidos y usa EXISTS sobre la relacion, no un
    /// JOIN: un articulo ligado a la categoria padre Y a una hija salia
    /// DUPLICADO con el join, y podia ocupar dos de las cinco tarjetas.
    private IQueryable<Models.ReadOnly.ContenidoRow> QueryPublicados(List<int> categoriaIds)
    {
        var q = _db.Contenidos.AsNoTracking().Where(EsPublicado);
        if (_respetarVigencias) q = q.Where(EstaVigente);
        return q
            .Where(c => _db.CategoriasRelacion
                .Any(r => r.IdContenido == c.Id
                       && !r.Borrado
                       && r.IdCategoria != null
                       && categoriaIds.Contains(r.IdCategoria.Value)))
            .OrderByDescending(c => c.FechaModificado ?? c.FechaCreado)
            .ThenByDescending(c => c.Id);
    }

    public async Task<List<ArticuloCardVm>> ObtenerCardsAsync(
        string categoriaSlug, int skip, int take, CancellationToken ct = default)
    {
        var cat = await BuscarCategoriaAsync(categoriaSlug, ct);
        if (cat is null) return new List<ArticuloCardVm>();

        var ids = await IdsDeArbolAsync(cat.Sequence, ct);

        var filas = await QueryPublicados(ids)
            .Skip(skip).Take(take)
            .Select(c => new { c.ContenidoTituloSlug, c.ContenidoTitulo, c.ContenidoTextoC })
            .ToListAsync(ct);

        return filas.Select((c, i) => new ArticuloCardVm
        {
            Posicion      = skip + i + 1,
            Slug          = c.ContenidoTituloSlug!,
            CategoriaSlug = categoriaSlug,
            Titulo        = c.ContenidoTitulo ?? "",
            Resumen       = Recortar(Limpiar(c.ContenidoTextoC ?? ""), 150)
        }).ToList();
    }

    public async Task<FilaVm?> ObtenerFilaAsync(string categoriaSlug, string titulo,
        string descripcion, string ancla, int top, CancellationToken ct = default)
    {
        var cat = await BuscarCategoriaAsync(categoriaSlug, ct);
        if (cat is null) return null;

        var ids   = await IdsDeArbolAsync(cat.Sequence, ct);
        var total = await QueryPublicados(ids).CountAsync(ct);
        var cards = await ObtenerCardsAsync(categoriaSlug, 0, top, ct);

        return new FilaVm
        {
            CategoriaSlug    = categoriaSlug,
            Ancla            = ancla,
            Titulo           = titulo,
            Descripcion      = descripcion,
            TotalEnCategoria = total,
            Cards            = cards
        };
    }

    public async Task<CategoriaVm?> ObtenerCategoriaAsync(
        string categoriaSlug, int pagina, CancellationToken ct = default)
    {
        var cat = await BuscarCategoriaAsync(categoriaSlug, ct);
        if (cat is null) return null;

        const int porPagina = 24;
        if (pagina < 1) pagina = 1;

        var ids   = await IdsDeArbolAsync(cat.Sequence, ct);
        var total = await QueryPublicados(ids).CountAsync(ct);
        var cards = await ObtenerCardsAsync(categoriaSlug, (pagina - 1) * porPagina, porPagina, ct);

        var descripcionBase = Recortar(Limpiar(cat.Descripcion), 260);

        var cfgCat = LeerConfigCategoria(categoriaSlug);

        return new CategoriaVm
        {
            Slug        = categoriaSlug,
            Nombre      = cat.Nombre,
            Descripcion = descripcionBase,
            H1          = Primero(cfgCat?.H1, cat.Nombre),
            MetaDescripcion = Primero(cfgCat?.MetaDescripcion, descripcionBase),
            // La intro es HTML redactado a mano en appsettings, pero pasa por el
            // MISMO sanitizador que el cuerpo de los articulos: una sola puerta
            // de entrada de HTML al sitio, sin excepciones de confianza.
            IntroHtml   = string.IsNullOrWhiteSpace(cfgCat?.IntroHtml)
                              ? ""
                              : _sanitizer.Sanitize(cfgCat!.IntroHtml!),
            Pagina      = pagina,
            PorPagina   = porPagina,
            Total       = total,
            Cards       = cards
        };
    }

    /// URLs publicadas de una categoria, para el sitemap. Devuelve exactamente
    /// lo que pasa por EsPublicado: si una tarjeta no se muestra y su URL da 404,
    /// tampoco puede aparecer aqui. Un sitemap con URLs muertas o con contenido
    /// no publico es de las cosas que Search Console marca de inmediato.
    public async Task<List<UrlSitemapVm>> ObtenerUrlsDeCategoriaAsync(
        string categoriaSlug, CancellationToken ct = default)
    {
        var cat = await BuscarCategoriaAsync(categoriaSlug, ct);
        if (cat is null) return new List<UrlSitemapVm>();

        var ids = await IdsDeArbolAsync(cat.Sequence, ct);

        var filas = await QueryPublicados(ids)
            .Select(c => new { c.ContenidoTituloSlug, c.FechaModificado, c.FechaCreado })
            .ToListAsync(ct);

        return filas.Select(f => new UrlSitemapVm
        {
            Ruta       = $"/{categoriaSlug}/{f.ContenidoTituloSlug}",
            Lastmod    = f.FechaModificado ?? f.FechaCreado,
            Prioridad  = "0.6",
            Frecuencia = "monthly"
        }).ToList();
    }

    // ── Sobreescritura por categoria (appsettings) ───────────────────────────
    // Sobreescribir cada campo por separado: se puede corregir solo el H1 y
    // dejar que la meta siga saliendo de la base. Una categoria que no este
    // declarada en appsettings sigue funcionando con los valores de la base.
    private CategoriaOptions? LeerConfigCategoria(string categoriaSlug)
        => _cfg.GetSection($"{CategoriaOptions.Seccion}:{categoriaSlug}")
               .Get<CategoriaOptions>();

    /// Nombre visible de una categoria: el H1 configurado si existe, el de la
    /// base si no. Unico punto donde se resuelve, para que la pagina de
    /// categoria, la miga del articulo y el modal digan siempre lo mismo.
    private string NombreDeCategoria(string categoriaSlug, string nombreBase)
        => Primero(LeerConfigCategoria(categoriaSlug)?.H1, nombreBase);

    private static string Primero(string? preferido, string alterno)
        => string.IsNullOrWhiteSpace(preferido) ? alterno : preferido.Trim();

    public async Task<ArticuloDetalleVm?> ObtenerArticuloAsync(
        string categoriaSlug, string slug, CancellationToken ct = default)
    {
        var cat = await BuscarCategoriaAsync(categoriaSlug, ct);
        if (cat is null) return null;

        var ids = await IdsDeArbolAsync(cat.Sequence, ct);

        // Mismo criterio de publicacion que los listados: si una tarjeta no se
        // muestra, su URL directa tampoco responde.
        var fila = await QueryPublicados(ids)
            .Where(c => c.ContenidoTituloSlug == slug)
            .Select(c => new
            {
                c.ContenidoTitulo,
                c.ContenidoTextoC,
                c.ContenidoTextoL,
                c.ContenidoTituloSlug,
                c.FechaModificado,
                c.FechaCreado
            })
            .FirstOrDefaultAsync(ct);

        if (fila is null) return null;

        return new ArticuloDetalleVm
        {
            Slug            = fila.ContenidoTituloSlug!,
            CategoriaSlug   = categoriaSlug,
            // El MISMO nombre que muestra la pagina de categoria. Si aqui usara
            // el de la base, la miga del articulo diria "Ayuda" y la pagina a la
            // que apunta diria "Informacion y ayuda": el BreadcrumbList nombraria
            // una pagina distinto de como se llama esa pagina.
            CategoriaNombre = NombreDeCategoria(categoriaSlug, cat.Nombre),
            Titulo          = fila.ContenidoTitulo ?? "",
            Resumen         = Recortar(Limpiar(fila.ContenidoTextoC ?? ""), 300),
            // El cuerpo viene del WYSIWYG del admin. Se sanitiza igual: el sitio
            // es publico y anonimo, y un script inyectado en un contenido viejo
            // bastaria para comprometer la pagina.
            CuerpoHtml      = _sanitizer.Sanitize(fila.ContenidoTextoL ?? ""),
            Actualizado     = fila.FechaModificado ?? fila.FechaCreado
        };
    }

    private static string Limpiar(string html)
        => System.Net.WebUtility.HtmlDecode(
               System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ")).Trim();

    private static string Recortar(string texto, int max)
    {
        texto = System.Text.RegularExpressions.Regex.Replace(texto, @"\s+", " ").Trim();
        if (texto.Length <= max) return texto;
        var corte = texto.LastIndexOf(' ', Math.Min(max, texto.Length - 1));
        return texto[..(corte > 0 ? corte : max)].TrimEnd(',', '.', ';') + "…";
    }
}
