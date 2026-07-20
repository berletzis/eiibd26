using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Platillos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Admin.Platillos
{
    /// <summary>
    /// Reporting SOLO LECTURA de las encuestas de tolerancia (/tolero/{slug}). Muestra, por
    /// ingrediente, el consenso crudo (Sí/A veces/No, n) + el % público que ve el paciente.
    /// No edita ni borra votos, no toca esquema. Precursor visible del bayesiano #16.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class EstadisticasToleranciaModel : PageModel
    {
        // DEBE COINCIDIR con EncuestaModel.MinVotos (guard del % público). MVP: duplicado con
        // comentario; centralizar si el cálculo crece.
        private const int MinVotos = 10;

        private readonly ApplicationDbContext _db;
        public EstadisticasToleranciaModel(ApplicationDbContext db) => _db = db;

        public List<Row> Rows { get; private set; } = new();

        /// <summary>0 = Todos · 1 = CUCI · 2 = Crohn.</summary>
        public int FiltroTipo { get; private set; }
        public bool MostrarTodos { get; private set; }

        public int TotalVotos => Rows.Sum(r => r.Total);
        public int IngredientesConVotos => Rows.Count(r => r.Total > 0);

        public async Task OnGetAsync(int? tipo, bool? todos)
        {
            FiltroTipo = tipo is 1 or 2 ? tipo.Value : 0;
            MostrarTodos = todos == true;
            Rows = await BuildRowsAsync(FiltroTipo, MostrarTodos);
        }

        public async Task<IActionResult> OnGetExportarCsvAsync(int? tipo, bool? todos)
        {
            var t = tipo is 1 or 2 ? tipo.Value : 0;
            var rows = await BuildRowsAsync(t, todos == true);

            var sb = new StringBuilder();
            sb.AppendLine("Ingrediente,Si,A veces,No,Total (n),% publico");
            foreach (var r in rows)
            {
                var pct = r.PctPublico.HasValue
                    ? r.PctPublico.Value.ToString(CultureInfo.InvariantCulture)
                    : "insuficiente";
                sb.Append('"').Append(r.Nombre.Replace("\"", "\"\"")).Append('"').Append(',')
                  .Append(r.Si).Append(',').Append(r.AVeces).Append(',').Append(r.No).Append(',')
                  .Append(r.Total).Append(',').Append(pct).Append('\n');
            }

            // BOM para que Excel abra los acentos correctamente.
            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var sufijo = t switch { 1 => "-cuci", 2 => "-crohn", _ => "" };
            return File(bytes, "text/csv", $"tolerancia{sufijo}.csv");
        }

        // Agregado de PlatTolerVoto por ingrediente. Un voto por (ingrediente, identidad); aquí solo
        // contamos. Los filtros CUCI/Crohn usan TipoEII, que solo tienen los votos de usuarios
        // registrados con tipo conocido (los anónimos quedan fuera del consenso por tipo, por diseño).
        private async Task<List<Row>> BuildRowsAsync(int tipo, bool todos)
        {
            var votosQ = _db.PlatTolerVotos.AsNoTracking();
            if (tipo is 1 or 2)
            {
                var tipoByte = (byte)tipo;
                votosQ = votosQ.Where(v => v.TipoEII == tipoByte);
            }

            var agg = await votosQ
                .GroupBy(v => v.IngredienteId)
                .Select(g => new
                {
                    IngredienteId = g.Key,
                    Si = g.Count(v => v.Tolera == PlatToleraNivel.Si),
                    AVeces = g.Count(v => v.Tolera == PlatToleraNivel.AVeces),
                    No = g.Count(v => v.Tolera == PlatToleraNivel.No)
                })
                .ToListAsync();

            var aggById = agg.ToDictionary(a => a.IngredienteId);

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
                var ids = agg.Select(a => a.IngredienteId).ToList();
                ings = await _db.PlatIngredientes.AsNoTracking()
                    .Where(i => ids.Contains(i.Id))
                    .Select(i => new NombreDto { Id = i.Id, Nombre = i.Nombre })
                    .ToListAsync();
            }

            return ings.Select(i =>
            {
                aggById.TryGetValue(i.Id, out var a);
                int si = a?.Si ?? 0, av = a?.AVeces ?? 0, no = a?.No ?? 0;
                int n = si + av + no;
                int binario = si + no;

                // MISMO cálculo que EncuestaModel.CargarResultadosAsync: Laplace (Sí+1)/((Sí+No)+2),
                // "A veces" fuera del binario, y guard n >= MinVotos con al menos un voto binario.
                int? pct = (n >= MinVotos && binario > 0)
                    ? (int)System.Math.Round((si + 1) / (double)(binario + 2) * 100)
                    : (int?)null;

                return new Row
                {
                    IngredienteId = i.Id,
                    Nombre = i.Nombre,
                    Si = si,
                    AVeces = av,
                    No = no,
                    Total = n,
                    PctPublico = pct
                };
            })
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.Nombre)
            .ToList();
        }

        private sealed class NombreDto { public int Id { get; set; } public string Nombre { get; set; } = ""; }

        public sealed class Row
        {
            public int IngredienteId { get; set; }
            public string Nombre { get; set; } = "";
            public int Si { get; set; }
            public int AVeces { get; set; }
            public int No { get; set; }
            public int Total { get; set; }
            /// <summary>% público (Laplace) o null si n &lt; 10 (guard, "insuficiente").</summary>
            public int? PctPublico { get; set; }
        }
    }
}
