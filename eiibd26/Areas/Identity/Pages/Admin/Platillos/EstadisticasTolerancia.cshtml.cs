using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eiibd26.Data;
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
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class EstadisticasToleranciaModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public EstadisticasToleranciaModel(ApplicationDbContext db) => _db = db;

        public List<Row> Rows { get; private set; } = new();
        public bool MostrarTodos { get; private set; }

        public int MinVotos => ToleranciaBayes.MinVotos;
        public double MaxAnchoIc => ToleranciaBayes.MaxAnchoIcPct;

        public int TotalVotos => Rows.Sum(r => r.Todos.Total);
        public int IngredientesConVotos => Rows.Count(r => r.Todos.Total > 0);
        public int VotosSinTipo => Rows.Sum(r => r.Todos.Total - r.Cuci.Total - r.Crohn.Total);

        public async Task OnGetAsync(bool? todos)
        {
            MostrarTodos = todos == true;
            Rows = await BuildRowsAsync(MostrarTodos);
        }

        public async Task<IActionResult> OnGetExportarCsvAsync(bool? todos)
        {
            var rows = await BuildRowsAsync(todos == true);

            var sb = new StringBuilder();
            sb.AppendLine("Ingrediente,Segmento,Si,A veces,No,Total (n),Media posterior %,IC bajo %,IC alto %,Se muestra");
            foreach (var r in rows)
            {
                foreach (var (segmento, s) in new[] { ("Todos", r.Todos), ("CUCI", r.Cuci), ("Crohn", r.Crohn) })
                {
                    sb.Append('"').Append(r.Nombre.Replace("\"", "\"\"")).Append('"').Append(',')
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
        // segmentos se arman en memoria a partir de ese cruce.
        private async Task<List<Row>> BuildRowsAsync(bool todos)
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

            // Nombres: si "mostrar todos", todos los ingredientes activos (incluye 0 votos, para ver
            // cobertura); si no, solo los que tienen al menos un voto.
            List<NombreDto> ings;
            if (todos)
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

                return new Row
                {
                    IngredienteId = i.Id,
                    Nombre = i.Nombre,
                    Todos = Armar(null),
                    Cuci = Armar(1),
                    Crohn = Armar(2)
                };
            })
            .OrderByDescending(r => r.Todos.Total)
            .ThenBy(r => r.Nombre)
            .ToList();
        }

        private sealed class NombreDto { public int Id { get; set; } public string Nombre { get; set; } = ""; }

        public sealed class Row
        {
            public int IngredienteId { get; set; }
            public string Nombre { get; set; } = "";
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
