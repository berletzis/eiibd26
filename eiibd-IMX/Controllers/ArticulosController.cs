using eiibd_IMX.Services;
using Microsoft.AspNetCore.Mvc;

namespace eiibd_IMX.Controllers;

public sealed class ArticulosController : Controller
{
    private readonly IContenidoService _contenido;
    public ArticulosController(IContenidoService contenido) => _contenido = contenido;

    /// Indice de la categoria. Es el destino de los "Ver todos" de la home y
    /// uno de los sitelinks de las campanas: tiene que existir de verdad.
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Categoria(string categoria, int p = 1, CancellationToken ct = default)
    {
        var vm = await _contenido.ObtenerCategoriaAsync(categoria, p, ct);
        if (vm is null) return NotFound();
        if (p > 1 && vm.Cards.Count == 0) return NotFound();
        return View(vm);
    }

    /// Pagina real e indexable del articulo. Es la URL a la que apunta el titulo
    /// de cada card y la que sirve de landing page para los grupos de anuncios.
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Detalle(string categoria, string slug, CancellationToken ct)
    {
        var a = await _contenido.ObtenerArticuloAsync(categoria, slug, ct);
        if (a is null) return NotFound();
        return View(a);
    }

    /// Fragmento HTML para el modal. Mismo contenido, sin layout.
    public async Task<IActionResult> Fragmento(string categoria, string slug, CancellationToken ct)
    {
        var a = await _contenido.ObtenerArticuloAsync(categoria, slug, ct);
        if (a is null) return NotFound();
        return PartialView("_ArticuloFragmento", a);
    }

    /// "Cargar mas": del 11 en adelante. Los primeros 10 ya vienen en el HTML.
    public async Task<IActionResult> Mas(string categoria, int skip = 10, int take = 10,
                                         CancellationToken ct = default)
    {
        if (take is < 1 or > 30) take = 10;
        var cards = await _contenido.ObtenerCardsAsync(categoria, skip, take, ct);
        return PartialView("_Cards", cards);
    }
}
