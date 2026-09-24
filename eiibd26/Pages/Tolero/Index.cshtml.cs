using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Models.Platillos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Tolero
{
    // Índice de la encuesta de tolerancia: /tolero sin ingrediente. Lista los ingredientes activos
    // agrupados por grupo, con enlace a /tolero/{slug}, y muestra la respuesta propia de quien mira
    // (logueado por UserId, anónimo por la cookie de la encuesta). SOLO LECTURA: votar sigue
    // viviendo en EncuestaModel.
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        // Misma cookie de dedup que EncuestaModel. Aquí solo se LEE, nunca se crea.
        private const string AnonCookie = "eii_tolero_anon";

        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<GrupoVm> Grupos { get; private set; } = new();
        public int TotalIngredientes { get; private set; }
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

            // 1 query: respuesta propia por ingrediente (sin N+1).
            var votos = await CargarVotosPropiosAsync();

            TotalIngredientes = filas.Count;
            TotalVotados = filas.Count(f => votos.ContainsKey(f.Id));

            // Agrupar en memoria respetando el orden ya traído (GroupBy conserva el orden de aparición).
            Grupos = filas
                .GroupBy(f => f.Grupo)
                .Select(g => new GrupoVm
                {
                    Nombre = g.Key,
                    Ingredientes = g.Select(f => new IngredienteVm
                    {
                        Nombre = f.Nombre,
                        Grupo = f.Grupo,
                        Slug = SlugHelper.GenerateSlug(f.Nombre),   // MISMO generador que resuelve /tolero/{slug}
                        MiVoto = votos.TryGetValue(f.Id, out var v) ? v : null
                    }).ToList()
                })
                .ToList();
        }

        private async Task<Dictionary<int, PlatToleraNivel>> CargarVotosPropiosAsync()
        {
            Guid? uid = null;
            if (User?.Identity?.IsAuthenticated ?? false)
                uid = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;
            EsAnonimo = uid == null;

            Guid? anon = null;
            if (uid == null && Request.Cookies.TryGetValue(AnonCookie, out var raw) && Guid.TryParse(raw, out var a))
                anon = a;   // Anónimo: solo si ya trae la cookie de un voto previo (no se crea en GET).

            if (uid == null && anon == null) return new Dictionary<int, PlatToleraNivel>();

            var filas = await _db.PlatTolerVotos.AsNoTracking()
                .Where(v => uid != null ? v.UserId == uid : v.AnonId == anon)
                .Select(v => new { v.IngredienteId, v.Tolera })
                .ToListAsync();

            // El UNIQUE filtrado garantiza un voto por identidad e ingrediente; el "último gana" es defensa.
            var dict = new Dictionary<int, PlatToleraNivel>();
            foreach (var f in filas) dict[f.IngredienteId] = f.Tolera;
            return dict;
        }

        public class GrupoVm
        {
            public string Nombre { get; set; } = "";
            public List<IngredienteVm> Ingredientes { get; set; } = new();

            /// <summary>Título de sección legible: "fruto-seco" → "Fruto seco". Solo presentación.</summary>
            public string Titulo
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(Nombre)) return "Otros";
                    var t = Nombre.Replace('-', ' ').Trim();
                    return char.ToUpper(t[0]) + t[1..];
                }
            }
        }

        public class IngredienteVm
        {
            public string Nombre { get; set; } = "";
            public string Grupo { get; set; } = "";
            public string Slug { get; set; } = "";
            public PlatToleraNivel? MiVoto { get; set; }

            /// <summary>"azúcar" → "Azúcar". Solo presentación: el catálogo guarda en minúscula (§7.1).</summary>
            public string NombreVisible => string.IsNullOrEmpty(Nombre) ? Nombre : char.ToUpper(Nombre[0]) + Nombre[1..];
        }
    }
}
