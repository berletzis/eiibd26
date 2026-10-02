namespace eiibd_IMX.Models.ReadOnly;

/// Proyeccion minima de dbo.contenidos. Solo columnas publicas:
/// nada de autor, firma, embeddings ni identificadores de usuario.
public sealed class ContenidoRow
{
    public int Id { get; set; }
    public string? ContenidoTitulo { get; set; }
    public string? ContenidoTextoC { get; set; }   // resumen / entradilla
    public string? ContenidoTextoL { get; set; }   // cuerpo HTML
    public string? ContenidoTituloSlug { get; set; }
    public string? URLImagenPrincipal { get; set; }
    public int? EstadoPublicacion { get; set; }
    public DateTime? ContenidoFechaInicio { get; set; }   // vigencia: desde
    public DateTime? ContenidoFechaFin { get; set; }      // vigencia: hasta
    public DateTime? FechaModificado { get; set; }
    public DateTime FechaCreado { get; set; }
    public bool Eliminado { get; set; }
}

public sealed class CategoriaRow
{
    public int Sequence { get; set; }
    public int? CategoriaPadre { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? CategoriaSlug { get; set; }
    public int? Orden { get; set; }
    public bool Borrado { get; set; }
}

public sealed class CategoriaRelacionRow
{
    public int Sequence { get; set; }
    public int IdContenido { get; set; }
    public int? IdCategoria { get; set; }
    public bool Borrado { get; set; }
    public bool? EsPrincipal { get; set; }
}
