namespace eiibd_IMX.Models.ViewModels;

/// Un eslabón de la ruta de navegación. Url null = eslabón actual: se pinta
/// como texto y en el JSON-LD va sin `item`, que es lo que pide schema.org
/// para el último elemento de un BreadcrumbList.
public sealed class MigaVm
{
    public string Texto { get; set; } = "";
    public string? Url { get; set; }
}

/// Modelo del partial _Breadcrumb: pinta la ruta visible Y el BreadcrumbList
/// de datos estructurados desde la MISMA lista, para que no puedan divergir
/// (que diverjan es exactamente lo que Google marca como marcado engañoso).
public sealed class BreadcrumbVm
{
    public List<MigaVm> Migas { get; set; } = new();

    public static BreadcrumbVm De(params MigaVm[] migas) => new() { Migas = migas.ToList() };
}

/// Una entrada del sitemap. La ruta es relativa; el host canónico se antepone
/// al serializar, para que el sitemap nunca pueda apuntar a otro dominio.
public sealed class UrlSitemapVm
{
    public string Ruta { get; set; } = "";
    public DateTime? Lastmod { get; set; }
    public string Prioridad { get; set; } = "0.6";
    public string Frecuencia { get; set; } = "monthly";
}
