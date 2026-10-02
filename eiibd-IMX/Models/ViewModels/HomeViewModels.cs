using eiibd_IMX.Models.Configuracion;

namespace eiibd_IMX.Models.ViewModels;

public sealed class ArticuloCardVm
{
    public int Posicion { get; set; }
    public string Slug { get; set; } = "";
    public string CategoriaSlug { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Resumen { get; set; } = "";
    public string Url => $"/{CategoriaSlug}/{Slug}";
}

public sealed class FilaVm
{
    public string CategoriaSlug { get; set; } = "";
    public string Ancla { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int TotalEnCategoria { get; set; }
    public List<ArticuloCardVm> Cards { get; set; } = new();
}

/// Tarjeta de un programa de la Fundación IMX. Enlaza a industrialesmx.org,
/// que es sitio de la misma organización: enlace normal, sin nofollow.
public sealed class ProgramaVm
{
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Url { get; set; } = "";
}

public sealed class ProyectoVm
{
    public string Eyebrow { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
}

public sealed class HomeVm
{
    public List<FilaVm> Filas { get; set; } = new();
    public List<ProgramaVm> Programas { get; set; } = new();
    public ProyectoVm Proyecto { get; set; } = new();
    /// Datos legales y de contacto. Los pinta el partial _DatosFiscales, el mismo
    /// que usa /transparencia: una sola definicion de la CLABE en todo el sitio.
    public OrganizacionOptions Organizacion { get; set; } = new();
}

public sealed class ArticuloDetalleVm
{
    public string Slug { get; set; } = "";
    public string CategoriaSlug { get; set; } = "";
    public string CategoriaNombre { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Resumen { get; set; } = "";
    public string CuerpoHtml { get; set; } = "";
    public DateTime Actualizado { get; set; }
    public string Url => $"/{CategoriaSlug}/{Slug}";
}

public sealed class CategoriaVm
{
    public string Slug { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";

    // ── Sobreescritura desde appsettings (seccion "Categorias") ──────────────
    // El nombre y la descripcion de la categoria viven en la base de EIIBD, que
    // aqui es de solo lectura: "Ayuda" como H1 y "Articulos de Ayuda en
    // Fundacion IMX" como meta no se pueden arreglar desde la base. Estos tres
    // campos permiten sobreescribirlos por configuracion; si no hay valor,
    // caen al de la base y una categoria nueva sigue funcionando sin tocar nada.
    public string H1 { get; set; } = "";
    public string MetaDescripcion { get; set; } = "";
    /// Ya sanitizado por ContenidoService: la vista lo pinta con Html.Raw.
    public string IntroHtml { get; set; } = "";

    public int Pagina { get; set; } = 1;
    public int PorPagina { get; set; } = 24;
    public int Total { get; set; }
    public List<ArticuloCardVm> Cards { get; set; } = new();

    public int TotalPaginas => Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)PorPagina);
    public bool HayPrevia => Pagina > 1;
    public bool HaySiguiente => Pagina < TotalPaginas;
    public string UrlPagina(int p) => p <= 1 ? $"/{Slug}" : $"/{Slug}?p={p}";
}
