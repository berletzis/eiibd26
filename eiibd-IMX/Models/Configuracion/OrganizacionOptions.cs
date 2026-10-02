namespace eiibd_IMX.Models.Configuracion;

/// Datos legales y de contacto de la Fundación. Viven en appsettings porque la
/// base de datos es de SOLO LECTURA y porque los consumen cuatro lugares
/// distintos —footer, /contacto, /transparencia y el JSON-LD de Organization—:
/// una sola definición evita que un teléfono cambie en tres de los cuatro.
public sealed class OrganizacionOptions
{
    public const string Seccion = "Organizacion";

    public string RazonSocial { get; set; } = "";
    public string NombreCorto { get; set; } = "";
    public string Rfc { get; set; } = "";
    public string Correo { get; set; } = "";

    /// Ruta relativa al logo. Vacía = no se emite `logo` en el JSON-LD:
    /// mejor omitir la propiedad que apuntar a una imagen que no existe.
    public string Logo { get; set; } = "";

    public string SitioFundacion { get; set; } = "";
    public string ComprobanteBancarioUrl { get; set; } = "";
    public string FiguraLegal { get; set; } = "";

    public List<string> Regimenes { get; set; } = new();
    public BancoOptions Banco { get; set; } = new();
    public DomicilioOptions Domicilio { get; set; } = new();
    public List<ZonaTelefonicaOptions> Telefonos { get; set; } = new();

    /// Todos los números en formato E.164, aplanados. Lo usa el JSON-LD.
    public IEnumerable<string> TodosLosTeles =>
        Telefonos.SelectMany(z => z.Numeros).Select(n => n.Tel)
                 .Where(t => !string.IsNullOrWhiteSpace(t));
}

/// Domicilio del responsable. Se guarda POR PARTES, no como una cadena suelta,
/// porque el JSON-LD lo necesita desarmado (`PostalAddress`) y las paginas lo
/// necesitan armado. Guardar las dos formas seria escribir el domicilio dos
/// veces y dejar que se desincronicen: aqui la cadena se compone de las partes.
public sealed class DomicilioOptions
{
    public string Calle { get; set; } = "";
    public string Localidad { get; set; } = "";
    public string Region { get; set; } = "";
    public string CodigoPostal { get; set; } = "";
    /// Codigo ISO de dos letras, solo para el JSON-LD.
    public string Pais { get; set; } = "";

    /// Sin domicilio configurado no se pinta la fila ni se emite `address`:
    /// mejor omitirlo que declarar un domicilio vacio.
    public bool Hay => !string.IsNullOrWhiteSpace(Calle);

    /// Una sola linea, como se lee en /transparencia y en el aviso de privacidad.
    public string Completo
    {
        get
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(Calle))        partes.Add(Calle.Trim());
            if (!string.IsNullOrWhiteSpace(Localidad))    partes.Add(Localidad.Trim());
            if (!string.IsNullOrWhiteSpace(Region))       partes.Add(Region.Trim());
            if (!string.IsNullOrWhiteSpace(CodigoPostal)) partes.Add("C.P. " + CodigoPostal.Trim());
            return string.Join(", ", partes);
        }
    }
}

public sealed class BancoOptions
{
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Cuenta { get; set; } = "";
    public string Clabe { get; set; } = "";
}

public sealed class ZonaTelefonicaOptions
{
    public string Zona { get; set; } = "";
    public List<TelefonoOptions> Numeros { get; set; } = new();
}

public sealed class TelefonoOptions
{
    /// Como se lee en pantalla: "55 7155-2094".
    public string Texto { get; set; } = "";
    /// Como se marca: "+525571552094". Va en el href del tel:.
    public string Tel { get; set; } = "";
}

/// Sobreescritura por categoría (R3). Permite darle a una página de categoría
/// un H1, una meta description y una intro propios sin tocar la base de EIIBD,
/// que es de solo lectura. Lo que no se declare aquí cae al valor de la base.
public sealed class CategoriaOptions
{
    public const string Seccion = "Categorias";

    public string? H1 { get; set; }
    public string? MetaDescripcion { get; set; }
    public string? IntroHtml { get; set; }
}
