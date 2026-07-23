using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Models.Platillos;
using eiibd26.Services.Platillos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Admin.Platillos
{
    /// <summary>
    /// Reporting SOLO LECTURA de las encuestas de tolerancia (/tolero/{slug}). Por ingrediente y por
    /// SEGMENTO (Todos · CUCI · Crohn) muestra el consenso crudo (Sí/A veces/No, n) más el modelo
    /// bayesiano #16: media posterior + IC 95%, con el mismo gate que decide si el paciente lo ve.
    ///
    /// La matemática NO vive aquí: viene de <see cref="ToleranciaBayes"/>, el mismo servicio que usa
    /// la encuesta pública. Un solo número por (segmento, ingrediente).
    ///
    /// Sutileza de los segmentos: solo los votos de usuarios REGISTRADOS con tipo de EII conocido
    /// tienen TipoEII. Los anónimos cuentan en "Todos" pero no en CUCI/Crohn, así que la n de segmento
    /// es siempre ≤ la total — y el gate se activa mucho más seguido ahí.
    ///
    /// El desglose por segmento es admin-only por ahora (#16 §7): en público solo se expone "Todos".
    ///
    /// Además del reporting, la página es el puesto de mando del ENVÍO de la encuesta: copiar la liga
    /// pública por ingrediente y marcar manualmente cuáles ya se mandaron (<see cref="PlatToleroEnvio"/>).
    /// Sigue sin mandar correos y sin tocar el cálculo: la liga se copia y se difunde por fuera.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class EstadisticasToleranciaModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public EstadisticasToleranciaModel(ApplicationDbContext db) => _db = db;

        public List<Row> Rows { get; private set; } = new();
        public bool MostrarTodos { get; private set; }
        /// <summary>Filtro "pendientes de enviar" (EnviadaEn IS NULL).</summary>
        public bool SoloPendientes { get; private set; }

        public int MinVotos => ToleranciaBayes.MinVotos;
        public double MaxAnchoIc => ToleranciaBayes.MaxAnchoIcPct;

        public int TotalVotos => Rows.Sum(r => r.Todos.Total);
        public int IngredientesConVotos => Rows.Count(r => r.Todos.Total > 0);
        public int VotosSinTipo => Rows.Sum(r => r.Todos.Total - r.Cuci.Total - r.Crohn.Total);
        public int Pendientes => Rows.Count(r => !r.Enviada);

        [TempData] public string? MensajeOk { get; set; }

        public async Task OnGetAsync(bool? todos, bool? pendientes)
        {
            MostrarTodos = todos == true;
            SoloPendientes = pendientes == true;
            Rows = await BuildRowsAsync(MostrarTodos, SoloPendientes);
        }

        // ── Control de envío ────────────────────────────────────────────────────────────────────
        // Handlers con parámetros NULLABLE a propósito: con nullable reference types un `int id`
        // es implícitamente [Required] y devuelve 400 si el binder no lo encuentra (regla del repo).

        public async Task<IActionResult> OnPostMarcarEnviadaAsync(int? id, bool? todos, bool? pendientes)
            => await GuardarEnvioAsync(id, DateTime.UtcNow, "Marcada como enviada.", todos, pendientes);

        public async Task<IActionResult> OnPostDeshacerEnvioAsync(int? id, bool? todos, bool? pendientes)
            => await GuardarEnvioAsync(id, null, "Vuelve a estar pendiente.", todos, pendientes);

        /// <summary>
        /// Upsert de la fila de envío del ingrediente. <paramref name="enviadaEn"/> null = deshacer.
        /// La fila se conserva al deshacer (no se borra): es una tabla chica y así el UNIQUE no pelea
        /// con un futuro re-marcado.
        /// </summary>
        private async Task<IActionResult> GuardarEnvioAsync(
            int? id, DateTime? enviadaEn, string mensaje, bool? todos, bool? pendientes)
        {
            // Preservar los filtros al volver (PRG): si no, el admin pierde su vista en cada marcado.
            var vuelta = new { todos = todos == true ? "true" : null, pendientes = pendientes == true ? "true" : null };

            if (id is not int ingredienteId || ingredienteId <= 0) return RedirectToPage(vuelta);

            var existe = await _db.PlatIngredientes.AsNoTracking().AnyAsync(i => i.Id == ingredienteId);
            if (!existe) return RedirectToPage(vuelta);

            var fila = await _db.PlatToleroEnvios.FirstOrDefaultAsync(e => e.IngredienteId == ingredienteId);
            if (fila == null)
            {
                fila = new PlatToleroEnvio { IngredienteId = ingredienteId };
                _db.PlatToleroEnvios.Add(fila);
            }

            fila.EnviadaEn = enviadaEn;
            fila.MarcadaPorUserId = enviadaEn == null ? null : UsuarioActual();

            try
            {
                await _db.SaveChangesAsync();
                MensajeOk = mensaje;
            }
            catch (DbUpdateException)
            {
                // Carrera de doble-envío contra el UNIQUE: la fila ya quedó, no es un error.
            }

            return RedirectToPage(vuelta);
        }

        private Guid? UsuarioActual()
            => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;

        /// <summary>
        /// URL pública absoluta de la encuesta. La base sale del host del request (NO se hardcodea),
        /// igual que el canonical de la propia encuesta.
        /// </summary>
        public string UrlEncuesta(string slug) => $"{Request.Scheme}://{Request.Host}/tolero/{slug}";

        public async Task<IActionResult> OnGetExportarCsvAsync(bool? todos, bool? pendientes)
        {
            var rows = await BuildRowsAsync(todos == true, pendientes == true);

            var sb = new StringBuilder();
            sb.AppendLine("Ingrediente,URL encuesta,Enviada en (UTC),Segmento,Si,A veces,No,Total (n),Media posterior %,IC bajo %,IC alto %,Se muestra");
            foreach (var r in rows)
            {
                var url = UrlEncuesta(r.Slug);
                var enviada = r.EnviadaEn?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "";
                foreach (var (segmento, s) in new[] { ("Todos", r.Todos), ("CUCI", r.Cuci), ("Crohn", r.Crohn) })
                {
                    sb.Append('"').Append(r.Nombre.Replace("\"", "\"\"")).Append('"').Append(',')
                      .Append(url).Append(',')
                      .Append(enviada).Append(',')
                      .Append(segmento).Append(',')
                      .Append(s.Si).Append(',').Append(s.AVeces).Append(',').Append(s.No).Append(',')
                      .Append(s.Total).Append(',')
                      .Append(s.Media.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                      .Append(s.CiBajo.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                      .Append(s.CiAlto.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                      .Append(s.Muestra ? "si" : "no").Append('\n');
                }
            }

            // BOM para que Excel abra los acentos correctamente.
            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", "tolerancia-bayes.csv");
        }

        // Agregado de PlatTolerVoto por (ingrediente, tipo de EII) en una sola pasada; los tres
        // segmentos se arman en memoria a partir de ese cruce. El estado de envío se cruza aparte.
        private async Task<List<Row>> BuildRowsAsync(bool todos, bool soloPendientes)
        {
            var agg = await _db.PlatTolerVotos.AsNoTracking()
                .GroupBy(v => new { v.IngredienteId, v.TipoEII })
                .Select(g => new
                {
                    g.Key.IngredienteId,
                    g.Key.TipoEII,
                    Si = g.Count(v => v.Tolera == PlatToleraNivel.Si),
                    AVeces = g.Count(v => v.Tolera == PlatToleraNivel.AVeces),
                    No = g.Count(v => v.Tolera == PlatToleraNivel.No)
                })
                .ToListAsync();

            var porIngrediente = agg.GroupBy(a => a.IngredienteId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Estado de envío por ingrediente (tabla chica: una fila por ingrediente ya marcado).
            var envios = await _db.PlatToleroEnvios.AsNoTracking()
                .ToDictionaryAsync(e => e.IngredienteId, e => e.EnviadaEn);

            // Slugs COLISIONADOS: dos ingredientes activos distintos pueden slugificar igual
            // ("Café"/"Cafe", "Queso."/"Queso"). /tolero/{slug} resuelve con FirstOrDefault, así que
            // uno de los dos se queda inalcanzable y su liga abriría la encuesta del OTRO. Se detecta
            // sobre el catálogo activo completo (no sobre la lista filtrada) y se avisa en la fila.
            var slugsDuplicados = (await _db.PlatIngredientes.AsNoTracking()
                    .Where(i => i.Activo).Select(i => i.Nombre).ToListAsync())
                .GroupBy(n => SlugHelper.GenerateSlug(n))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            // Nombres: si "mostrar todos", todos los ingredientes activos (incluye 0 votos, para ver
            // cobertura); si no, solo los que tienen al menos un voto.
            //
            // "Pendientes de enviar" IMPLICA el universo completo: lo que falta por mandar son
            // justamente los que todavía no tienen votos, y con el universo chico la lista saldría
            // casi vacía — el filtro no serviría para lo único que sirve, que es trabajar la cola.
            List<NombreDto> ings;
            if (todos || soloPendientes)
            {
                ings = await _db.PlatIngredientes.AsNoTracking()
                    .Where(i => i.Activo)
                    .Select(i => new NombreDto { Id = i.Id, Nombre = i.Nombre })
                    .ToListAsync();
            }
            else
            {
                var ids = porIngrediente.Keys.ToList();
                ings = await _db.PlatIngredientes.AsNoTracking()
                    .Where(i => ids.Contains(i.Id))
                    .Select(i => new NombreDto { Id = i.Id, Nombre = i.Nombre })
                    .ToListAsync();
            }

            return ings.Select(i =>
            {
                porIngrediente.TryGetValue(i.Id, out var grupos);
                grupos ??= new();

                Segmento Armar(byte? tipo) // tipo null = "Todos" (no filtra)
                {
                    var sel = tipo == null ? grupos : grupos.Where(g => g.TipoEII == tipo).ToList();
                    return Segmento.Crear(sel.Sum(g => g.Si), sel.Sum(g => g.AVeces), sel.Sum(g => g.No));
                }

                envios.TryGetValue(i.Id, out var enviadaEn);
                // MISMO generador que usa la encuesta para resolver /tolero/{slug}. Si estos dos
                // divergieran, la liga que copia el admin daría 404: fuente única, a propósito.
                var slug = SlugHelper.GenerateSlug(i.Nombre);

                return new Row
                {
                    IngredienteId = i.Id,
                    Nombre = i.Nombre,
                    Slug = slug,
                    SlugColisionado = slugsDuplicados.Contains(slug),
                    EnviadaEn = enviadaEn,
                    Todos = Armar(null),
                    Cuci = Armar(1),
                    Crohn = Armar(2)
                };
            })
            .Where(r => !soloPendientes || !r.Enviada)
            .OrderByDescending(r => r.Todos.Total)
            .ThenBy(r => r.Nombre)
            .ToList();
        }

        private sealed class NombreDto { public int Id { get; set; } public string Nombre { get; set; } = ""; }

        public sealed class Row
        {
            public int IngredienteId { get; set; }
            public string Nombre { get; set; } = "";
            /// <summary>Slug público, generado con el MISMO SlugHelper que resuelve /tolero/{slug}.</summary>
            public string Slug { get; set; } = "";
            /// <summary>
            /// Otro ingrediente activo produce este mismo slug: la liga es ambigua y puede abrir la
            /// encuesta del otro. No se rompe nada, pero no conviene difundirla hasta desempatar el nombre.
            /// </summary>
            public bool SlugColisionado { get; set; }
            /// <summary>Última vez marcada como enviada (UTC); null = pendiente.</summary>
            public DateTime? EnviadaEn { get; set; }
            public bool Enviada => EnviadaEn.HasValue;
            /// <summary>Todos los votos, incluidos los anónimos. Es el único segmento que ve el paciente.</summary>
            public Segmento Todos { get; set; } = Segmento.Vacio;
            /// <summary>Solo votos de registrados con TipoEII = 1.</summary>
            public Segmento Cuci { get; set; } = Segmento.Vacio;
            /// <summary>Solo votos de registrados con TipoEII = 2.</summary>
            public Segmento Crohn { get; set; } = Segmento.Vacio;
        }

        /// <summary>Consenso de un (ingrediente, segmento) con su posterior ya resuelto.</summary>
        public sealed class Segmento
        {
            public static readonly Segmento Vacio = Crear(0, 0, 0);

            public int Si { get; private init; }
            public int AVeces { get; private init; }
            public int No { get; private init; }
            /// <summary>n del segmento, incluyendo "A veces".</summary>
            public int Total { get; private init; }

            public double Media { get; private init; }
            public double CiBajo { get; private init; }
            public double CiAlto { get; private init; }
            public double Ancho => CiAlto - CiBajo;

            /// <summary>¿Este segmento pasa el gate? En "Todos" es exactamente lo que decide el % público.</summary>
            public bool Muestra { get; private init; }

            public int MediaInt => (int)System.Math.Round(Media);
            public int CiBajoInt => (int)System.Math.Round(CiBajo);
            public int CiAltoInt => (int)System.Math.Round(CiAlto);

            public static Segmento Crear(int si, int aveces, int no)
            {
                int n = si + aveces + no;
                var e = ToleranciaBayes.Estimar(si, no);
                return new Segmento
                {
                    Si = si,
                    AVeces = aveces,
                    No = no,
                    Total = n,
                    Media = e.MediaPct,
                    CiBajo = e.CiBajoPct,
                    CiAlto = e.CiAltoPct,
                    Muestra = ToleranciaBayes.PasaGate(e, n)
                };
            }
        }
    }
}
