using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using eiibd26.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.FileProviders;

namespace eiibd26.Services.Platillos
{
    /// <summary>
    /// Resuelve el ícono ilustrado de un alimento y lo devuelve INLINE (no &lt;img&gt;), para que el
    /// glifo herede el color del grupo vía <c>currentColor</c> — un &lt;img&gt; no hereda `color`.
    ///
    /// Cadena de fallback (acordada con el owner): ingrediente → su grupo → genérico.
    /// Un ingrediente sin ícono propio cae al de su grupo, que ya comunica algo; solo si tampoco
    /// existe cae al genérico. Nunca se rompe la moneda.
    ///
    /// Los SVG los entrega Claude Design (REQ 1) y se sueltan en wwwroot; no hay que tocar código
    /// para darlos de alta — el archivo aparece y el ícono empieza a salir solo.
    /// </summary>
    public interface IIconoAlimentoService
    {
        /// <summary>SVG inline del alimento. <paramref name="nombreIngrediente"/> null/vacío = moneda de grupo.</summary>
        IHtmlContent Icono(string? nombreIngrediente, string? nombreGrupo, string cssClass = IconoAlimentoService.ClaseIconoPorDefecto);

        /// <summary>Slug del grupo para la clase de color <c>eii-grupo-{slug}</c>. Vacío → "otro".</summary>
        string GrupoSlug(string? nombreGrupo);
    }

    public class IconoAlimentoService : IIconoAlimentoService
    {
        public const string ClaseIconoPorDefecto = "eii-moneda__icono";

        private const string DirIngredientes = "img/ingredientes";
        private const string DirGrupos = "img/grupos";
        private const string ArchivoGenerico = "img/ingredientes/_fallback.svg";
        private const string GrupoPorDefecto = "otro";

        private readonly IFileProvider _wwwroot;

        // Caché de SVG ya preparados, invalidada por (tamaño, fecha de modificación) del archivo:
        // soltar un SVG nuevo NO exige reiniciar la app.
        private readonly ConcurrentDictionary<string, Entrada> _cache = new();
        private sealed record Entrada(long Length, DateTimeOffset LastModified, string Svg);

        public IconoAlimentoService(IWebHostEnvironment env) => _wwwroot = env.WebRootFileProvider;

        public string GrupoSlug(string? nombreGrupo)
        {
            if (string.IsNullOrWhiteSpace(nombreGrupo)) return GrupoPorDefecto;
            var slug = SlugHelper.GenerateSlug(nombreGrupo);
            // GenerateSlug devuelve "pregunta" como último recurso; aquí eso significa "sin grupo usable".
            return string.IsNullOrWhiteSpace(slug) || slug == "pregunta" ? GrupoPorDefecto : slug;
        }

        public IHtmlContent Icono(string? nombreIngrediente, string? nombreGrupo, string cssClass = ClaseIconoPorDefecto)
        {
            var clase = string.IsNullOrWhiteSpace(cssClass) ? ClaseIconoPorDefecto : cssClass;

            if (!string.IsNullOrWhiteSpace(nombreIngrediente))
            {
                var slugIng = SlugHelper.GenerateSlug(nombreIngrediente);
                if (!string.IsNullOrWhiteSpace(slugIng))
                {
                    var svg = Leer($"{DirIngredientes}/{slugIng}.svg", clase);
                    if (svg != null) return new HtmlString(svg);
                }
            }

            var slugGrupo = GrupoSlug(nombreGrupo);
            var svgGrupo = Leer($"{DirGrupos}/{slugGrupo}.svg", clase);
            if (svgGrupo != null) return new HtmlString(svgGrupo);

            return new HtmlString(Leer(ArchivoGenerico, clase) ?? "");
        }

        /// <summary>Lee y prepara un SVG de wwwroot. Null si no existe o no es un SVG utilizable.</summary>
        private string? Leer(string rutaRelativa, string cssClass)
        {
            var info = _wwwroot.GetFileInfo(rutaRelativa);
            if (!info.Exists || info.IsDirectory) return null;

            var key = $"{rutaRelativa}|{cssClass}";
            if (_cache.TryGetValue(key, out var hit)
                && hit.Length == info.Length && hit.LastModified == info.LastModified)
                return hit.Svg;

            string crudo;
            try
            {
                using var stream = info.CreateReadStream();
                using var reader = new StreamReader(stream);
                crudo = reader.ReadToEnd();
            }
            catch (IOException)
            {
                // Archivo en uso / medio caído: no es motivo para tumbar la página, cae al siguiente eslabón.
                return null;
            }

            var svg = Preparar(crudo, cssClass);
            if (svg == null) return null;

            _cache[key] = new Entrada(info.Length, info.LastModified, svg);
            return svg;
        }

        private static readonly RegexOptions Opts = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled;
        private static readonly Regex RxXmlDecl = new(@"<\?xml.*?\?>", Opts);
        private static readonly Regex RxDoctype = new(@"<!DOCTYPE.*?>", Opts);
        private static readonly Regex RxComentario = new(@"<!--.*?-->", Opts);
        private static readonly Regex RxScript = new(@"<script\b.*?</script\s*>", Opts);
        private static readonly Regex RxSvgOpen = new(@"<svg\b[^>]*>", Opts);

        private static readonly ConcurrentDictionary<string, Regex> _rxAtributo = new();
        private static Regex Atributo(string nombre) => _rxAtributo.GetOrAdd(nombre,
            n => new Regex($@"\s{Regex.Escape(n)}\s*=\s*(""[^""]*""|'[^']*')", Opts));

        /// <summary>
        /// Deja el SVG listo para inyectar: fuera declaración XML/DOCTYPE/comentarios/scripts, y en la
        /// etiqueta raíz fuera width/height (manda el CSS) más class + aria-hidden (el texto real vive
        /// en el pie de la moneda, así que el glifo es decorativo).
        /// </summary>
        private static string? Preparar(string crudo, string cssClass)
        {
            var svg = RxScript.Replace(
                        RxComentario.Replace(
                          RxDoctype.Replace(
                            RxXmlDecl.Replace(crudo, ""), ""), ""), "").Trim();

            var m = RxSvgOpen.Match(svg);
            if (!m.Success) return null;

            var open = m.Value;
            foreach (var attr in new[] { "class", "width", "height", "aria-hidden", "focusable" })
                open = Atributo(attr).Replace(open, "");

            open = open.TrimEnd('>').TrimEnd().TrimEnd('/').TrimEnd()
                 + $" class=\"{HtmlEncoder.Default.Encode(cssClass)}\" aria-hidden=\"true\" focusable=\"false\">";

            return svg.Remove(m.Index, m.Length).Insert(m.Index, open);
        }
    }

    /// <summary>Modelo del partial <c>_MonedaAlimento</c>: la moneda circular con el ícono del alimento.</summary>
    public class MonedaAlimentoVm
    {
        /// <summary>Nombre del ingrediente. Null/vacío = moneda del grupo (catálogo, filtros).</summary>
        public string? Nombre { get; set; }
        /// <summary>Nombre del grupo — da el color y el segundo eslabón del fallback.</summary>
        public string? Grupo { get; set; }
        /// <summary>Modificador de tamaño opcional: <c>eii-moneda--sm</c> / <c>eii-moneda--lg</c>.</summary>
        public string? ExtraClass { get; set; }
    }
}
