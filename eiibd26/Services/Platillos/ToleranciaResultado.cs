using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Platillos;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services.Platillos
{
    /// <summary>
    /// Resultado comunitario de tolerancia ("Todos") de UN ingrediente, ya resuelto: conteos + media
    /// posterior + IC 95% + gate. Solo lectura, listo para pintar. Los porcentajes son 0..100.
    /// </summary>
    public sealed class ToleranciaResultado
    {
        public int CountSi { get; init; }
        public int CountAVeces { get; init; }
        public int CountNo { get; init; }
        /// <summary>n total, incluyendo "A veces".</summary>
        public int TotalRespuestas { get; init; }

        /// <summary>¿Pasa el gate (n≥MinVotos y ancho IC ≤ máximo)? Si es false, NUNCA se muestra el %.</summary>
        public bool MostrarPorcentaje { get; init; }
        /// <summary>Media posterior redondeada — el "X % lo tolera bien". Solo válido si MostrarPorcentaje.</summary>
        public int PorcentajeTolera { get; init; }
        public int CiBajo { get; init; }
        public int CiAlto { get; init; }
    }

    /// <summary>
    /// FUENTE ÚNICA del resultado comunitario "Todos". Antes vivía inline en
    /// <c>Tolero/Encuesta.CargarResultadosAsync</c>; se extrajo aquí para que la encuesta pública
    /// (/tolero) y la ficha de ingrediente (/Platillos/Ingrediente) NUNCA muestren cifras distintas.
    ///
    /// El cálculo puro (posterior, IC, gate) sigue en <see cref="ToleranciaBayes"/> y NO se toca; esto
    /// solo carga los conteos del ingrediente y los pasa por él. Segmento "Todos" (sin filtrar por EII).
    /// </summary>
    public static class ToleranciaResultadoCalculo
    {
        public static async Task<ToleranciaResultado> ParaIngredienteAsync(ApplicationDbContext db, int ingredienteId)
        {
            var grupos = await db.PlatTolerVotos.AsNoTracking()
                .Where(v => v.IngredienteId == ingredienteId)
                .GroupBy(v => v.Tolera)
                .Select(g => new { Nivel = g.Key, Count = g.Count() })
                .ToListAsync();

            int si = grupos.FirstOrDefault(g => g.Nivel == PlatToleraNivel.Si)?.Count ?? 0;
            int aveces = grupos.FirstOrDefault(g => g.Nivel == PlatToleraNivel.AVeces)?.Count ?? 0;
            int no = grupos.FirstOrDefault(g => g.Nivel == PlatToleraNivel.No)?.Count ?? 0;
            int total = si + aveces + no;

            var est = ToleranciaBayes.Estimar(si, no);
            bool mostrar = ToleranciaBayes.PasaGate(est, total);

            return new ToleranciaResultado
            {
                CountSi = si,
                CountAVeces = aveces,
                CountNo = no,
                TotalRespuestas = total,
                MostrarPorcentaje = mostrar,
                PorcentajeTolera = mostrar ? est.MediaRedondeada : 0,
                CiBajo = mostrar ? est.CiBajoRedondeado : 0,
                CiAlto = mostrar ? est.CiAltoRedondeado : 0
            };
        }
    }
}
