using eiibd_IMX.Data;
using eiibd_IMX.Models.Configuracion;
using eiibd_IMX.Services;
using Ganss.Xss;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

// Diagnostico de arranque: una excepcion aqui sale como 0xE0434352 sin mas
// detalle. La volcamos a startup-error.log para poder leerla.
var logArranque = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
try { if (File.Exists(logArranque)) File.Delete(logArranque); } catch { }

try
{

var builder = WebApplication.CreateBuilder(args);

// ── Conexion ────────────────────────────────────────────────────────────────
// Se toma de User Secrets. El .csproj declara el MISMO UserSecretsId que eiibd26,
// asi que ambos proyectos leen la misma cadena sin copiarla a ningun archivo.
// Antes de publicar: cambiar a un login `imx_reader` con GRANT SELECT unicamente
// sobre las vistas publicas y DENY sobre dbo. Ver plan-sitio-fundacion.
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection (llego nula o vacia). " +
        "Verifica que exista en los User Secrets del UserSecretsId declarado en el .csproj, " +
        "o cargala con: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\" " +
        "--project eiibd-IMX/eiibd-IMX.csproj");

builder.Services.AddDbContext<ImxDbContext>(o =>
    o.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(3))
     .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

// ── Sanitizado de HTML ──────────────────────────────────────────────────────
builder.Services.AddSingleton(_ =>
{
    var s = new HtmlSanitizer();
    s.AllowedTags.Clear();
    foreach (var t in new[]{"p","h2","h3","h4","ul","ol","li","strong","em","b","i",
                            "a","blockquote","br","table","thead","tbody","tr","th","td",
                            "figure","figcaption","img","sup","sub"})
        s.AllowedTags.Add(t);

    s.AllowedAttributes.Clear();
    foreach (var a in new[]{"href","src","alt","title","width","height","colspan","rowspan","rel","target"})
        s.AllowedAttributes.Add(a);

    s.AllowedSchemes.Clear();
    s.AllowedSchemes.Add("https");
    s.AllowedSchemes.Add("http");
    s.AllowedSchemes.Add("mailto");

    // Todo enlace externo sale sin transmitir autoridad y sin exponer el opener.
    s.PostProcessNode += (_, e) =>
    {
        if (e.Node is AngleSharp.Html.Dom.IHtmlAnchorElement a &&
            a.GetAttribute("href")?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true)
        {
            a.SetAttribute("rel", "nofollow noopener");
            a.SetAttribute("target", "_blank");
        }
    };
    return s;
});

builder.Services.Configure<OrganizacionOptions>(
    builder.Configuration.GetSection(OrganizacionOptions.Seccion));

builder.Services.AddScoped<IContenidoService, ContenidoService>();
// En Development las vistas se recompilan al vuelo: editar un .cshtml se ve
// con F5, sin rebuild. En produccion se compilan al publicar, como debe ser.
var mvc = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment()) mvc.AddRazorRuntimeCompilation();
builder.Services.AddResponseCompression();
builder.Services.AddMemoryCache();
builder.Services.Configure<RouteOptions>(o =>
{
    o.LowercaseUrls = true;
    o.LowercaseQueryStrings = true;
    o.AppendTrailingSlash = false;
});

var app = builder.Build();

// ── Detras de Cloudflare ────────────────────────────────────────────────────
// CF termina el TLS y habla con el origen por su cuenta. Sin esto la app cree
// que TODA peticion llego por HTTP: UseHttpsRedirection redirige a HTTPS, CF
// vuelve a pedir al origen, y se arma un bucle infinito de redirecciones.
// Va primero en el pipeline, antes que cualquier otro middleware.
var reenvio = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// Se confia en el proxy sin lista blanca porque el origen solo debe ser
// alcanzable desde Cloudflare. Si el firewall no esta restringido a los rangos
// de CF, cualquiera que llegue por IP podria falsear estas cabeceras.
reenvio.KnownNetworks.Clear();
reenvio.KnownProxies.Clear();
app.UseForwardedHeaders(reenvio);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute("home", "", new { controller = "Home", action = "Index" });
app.MapControllerRoute("error", "error/{code?}", new { controller = "Home", action = "Error" });

// Endpoints del modal y del "cargar mas". No indexables por diseno: son
// fragmentos, la version indexable es la ruta /{categoria}/{slug} de abajo.
app.MapControllerRoute("api-articulo", "api/articulo/{categoria}/{slug}",
    new { controller = "Articulos", action = "Fragmento" });
app.MapControllerRoute("api-mas", "api/mas/{categoria}",
    new { controller = "Articulos", action = "Mas" });

// ── Rutas con segmento literal ──────────────────────────────────────────────
// TODAS van ANTES de los dos catch-all de abajo. Registrada despues, cualquiera
// de estas entraria como {categoria}, no existiria en la base y daria 404.
app.MapControllerRoute("robots", "robots.txt",
    new { controller = "Seo", action = "Robots" });
app.MapControllerRoute("sitemap", "sitemap.xml",
    new { controller = "Seo", action = "Sitemap" });

app.MapControllerRoute("quienes-somos", "quienes-somos",
    new { controller = "Institucional", action = "QuienesSomos" });
app.MapControllerRoute("transparencia", "transparencia",
    new { controller = "Institucional", action = "Transparencia" });
app.MapControllerRoute("contacto", "contacto",
    new { controller = "Institucional", action = "Contacto" });
app.MapControllerRoute("aviso-de-privacidad", "aviso-de-privacidad",
    new { controller = "Institucional", action = "AvisoPrivacidad" });

// Catch-all de dos segmentos. SIEMPRE al final: valida que la categoria exista
// y devuelve 404 real si no. Un catch-all que responde 200 a cualquier cosa
// genera paginas basura infinitas y arruina el indice de un dominio nuevo.
app.MapControllerRoute("articulo", "{categoria}/{slug}",
    new { controller = "Articulos", action = "Detalle" });

// Indice de categoria: /{categoria}. Un solo segmento, asi que no compite con
// la ruta de arriba. Los literales ("error") ganan por precedencia de routing.
app.MapControllerRoute("categoria", "{categoria}",
    new { controller = "Articulos", action = "Categoria" });

app.Run();

}
catch (Exception ex)
{
    var detalle = DateTime.Now.ToString("u") + Environment.NewLine + ex;
    for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        detalle += Environment.NewLine + "--- INNER ---" + Environment.NewLine + inner;

    try { File.WriteAllText(logArranque, detalle); } catch { }
    Console.Error.WriteLine("=== FALLO DE ARRANQUE ===");
    Console.Error.WriteLine(detalle);
    throw;
}
