using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Tolero
{
    // Índice de la encuesta de tolerancia: /tolero sin ingrediente. Lista los ingredientes activos
    // con enlace a /tolero/{slug} y marca en cuáles ya votó quien mira (logueado por UserId, anónimo
    // por la cookie de la encuesta). SOLO LECTURA: votar sigue viviendo en EncuestaModel.
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        // Misma cookie de dedup que EncuestaModel. Aquí solo se LEE, nunca se crea.
        private const string AnonCookie = "eii_tolero_anon";

        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<IngredienteVm> Ingredientes { get; private set; } = new();
        public bool EsAnonimo { get; private set; }
        public int TotalVotados { get; private set; }

        public async Task OnGetAsync()
        {
            // 1 query: ingredientes activos + grupo. Orden: grupo (Orden del catálogo) y luego nombre.
            var filas = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => i.Activo)
                .Select(i => new
                {
                    i.Id,
                    i.Nombre,
                    Grupo = i.Grupo != null ? i.Grupo.Nombre : "",
                    GrupoOrden = i.Grupo != null ? i.Grupo.Orden : int.MaxValue
                })
                .OrderBy(i => i.GrupoOrden).ThenBy(i => i.Grupo).ThenBy(i => i.Nombre)
                .ToListAsync();

            // 1 query: ids de ingredientes en los que ya votó esta identidad (sin N+1).
            var votados = await CargarVotadosAsync();
            TotalVotados = filas.Count(f => votados.Contains(f.Id));

            Ingredientes = filas.Select(f => new IngredienteVm
            {
                Nombre = f.Nombre,
                Grupo = f.Grupo,
                Slug = SlugHelper.GenerateSlug(f.Nombre),   // MISMO generador que resuelve /tolero/{slug}
                YaVoto = votados.Contains(f.Id)
            }).ToList();
        }

        private async Task<HashSet<int>> CargarVotadosAsync()
        {
            Guid? uid = null;
            if (User?.Identity?.IsAuthenticated ?? false)
                uid = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;
            EsAnonimo = uid == null;

            if (uid != null)
                return (await _db.PlatTolerVotos.AsNoTracking()
                    .Where(v => v.UserId == uid)
                    .Select(v => v.IngredienteId)
                    .ToListAsync()).ToHashSet();

            // Anónimo: solo si ya trae la cookie de un voto previo (no se crea en GET).
            if (Request.Cookies.TryGetValue(AnonCookie, out var raw) && Guid.TryParse(raw, out var anon))
                return (await _db.PlatTolerVotos.AsNoTracking()
                    .Where(v => v.AnonId == anon)
                    .Select(v => v.IngredienteId)
                    .ToListAsync()).ToHashSet();

            return new HashSet<int>();
        }

        public class IngredienteVm
        {
            public string Nombre { get; set; } = "";
            public string Grupo { get; set; } = "";
            public string Slug { get; set; } = "";
            public bool YaVoto { get; set; }
        }
    }
}
