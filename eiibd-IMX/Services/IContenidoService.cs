using eiibd_IMX.Models.ViewModels;

namespace eiibd_IMX.Services;

public interface IContenidoService
{
    Task<FilaVm?> ObtenerFilaAsync(string categoriaSlug, string titulo, string descripcion,
                                   string ancla, int top, CancellationToken ct = default);

    Task<List<ArticuloCardVm>> ObtenerCardsAsync(string categoriaSlug, int skip, int take,
                                                 CancellationToken ct = default);

    Task<ArticuloDetalleVm?> ObtenerArticuloAsync(string categoriaSlug, string slug,
                                                  CancellationToken ct = default);

    Task<CategoriaVm?> ObtenerCategoriaAsync(string categoriaSlug, int pagina,
                                             CancellationToken ct = default);

    /// Rutas de los articulos publicados de una categoria, para el sitemap.
    Task<List<UrlSitemapVm>> ObtenerUrlsDeCategoriaAsync(string categoriaSlug,
                                                         CancellationToken ct = default);
}
