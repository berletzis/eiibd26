using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Models;
using eiibd26.Models.Platillos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Tolero
{
    // Encuesta de tolerancia de UNA pregunta ("¿Toleras el queso?"). Ruta /tolero/{slug}, el mismo
    // slug que /Platillos/Ingrediente/{slug}. Pública y ANÓNIMA a propósito (alcance viral, no rating
    // médico como M-4): dedup por cookie + rate-limit por IP.
    //
    // Encuadre del producto: EXPERIENCIA de la comunidad, NO consejo médico. Nunca "deberías poder
    // comerlo". El % es un dato comunitario suavizado (Laplace), NO una cifra clínica ni el bayesiano
    // (#16) — ese reemplaza este cálculo después, con la data que ya guardamos aquí.
    [AllowAnonymous]
    [EnableRateLimiting("tolero")]
    public class EncuestaModel : PageModel
    {
        // Mínimo de respuestas para revelar el porcentaje. Parametrizable — evita el "100% con 2 votos".
        private const int MinVotos = 10;
        private const string AnonCookie = "eii_tolero_anon";

        private readonly ApplicationDbContext _db;
        public EncuestaModel(ApplicationDbContext db) => _db = db;

        public string Slug { get; private set; } = "";
        public int IngredienteId { get; private set; }
        public string Nombre { get; private set; } = "";

        // Estado de sesión / voto propio.
        public bool EsAnonimo { get; private set; }
        public bool YaVoto { get; private set; }
        public PlatToleraNivel? MiVoto { get; private set; }

        // Opción resaltada por el link de correo (?intent=si|aveces|no). NUNCA vota sola (anti-prefetch).
        public string? IntentDestacado { get; private set; }

        // Resultado de la comunidad.
        public int CountSi { get; private set; }
        public int CountAVeces { get; private set; }
        public int CountNo { get; private set; }
        public int TotalRespuestas { get; private set; }
        public bool MostrarPorcentaje { get; private set; }
        public int PorcentajeTolera { get; private set; }

        [TempData] public string? ErrorVoto { get; set; }

        public async Task<IActionResult> OnGetAsync(string? slug, string? intent)
        {
            if (!await ResolverIngredienteAsync(slug)) return NotFound();

            IntentDestacado = NormalizarIntent(intent);
            await CargarVotoPropioAsync();
            await CargarResultadosAsync();
            return Page();
        }

        // Voto: SOLO por POST (el GET nunca vota → los escáneres de correo que precargan el link no
        // inflan el conteo). Upsert por UserId (logueado) o AnonId (cookie): un voto por identidad,
        // cambiable. Patrón PRG: tras guardar, redirige al GET, que muestra el resultado.
        public async Task<IActionResult> OnPostVotarAsync(string? slug, int? tolera)
        {
            if (!await ResolverIngredienteAsync(slug)) return NotFound();

            if (tolera is not (1 or 2 or 3))
            {
                ErrorVoto = "Elige una opción para responder.";
                return RedirectToPage(new { slug = Slug });
            }
            var nivel = (PlatToleraNivel)tolera.Value;

            Guid? uid = UsuarioActual();
            Guid? anon = uid == null ? ObtenerOCrearAnonId() : null;

            // Condición principal CRUDA + tipo de EII derivado (solo si está logueado). CondicionIdPrincipal
            // es la fuente de verdad para #16; TipoEII es denormalización recomputable.
            int? condId = null;
            byte? tipoEii = null;
            if (uid is Guid u)
                (condId, tipoEii) = await ResolverCondicionAsync(u);

            var existente = uid != null
                ? await _db.PlatTolerVotos.FirstOrDefaultAsync(v => v.IngredienteId == IngredienteId && v.UserId == uid)
                : await _db.PlatTolerVotos.FirstOrDefaultAsync(v => v.IngredienteId == IngredienteId && v.AnonId == anon);

            if (existente != null)
            {
                existente.Tolera = nivel;
                existente.FechaVoto = DateTime.UtcNow;
                if (uid != null)
                {
                    existente.CondicionIdPrincipal = condId;
                    existente.TipoEII = tipoEii;
                }
            }
            else
            {
                _db.PlatTolerVotos.Add(new PlatTolerVoto
                {
                    IngredienteId = IngredienteId,
                    UserId = uid,
                    AnonId = anon,
                    Tolera = nivel,
                    CondicionIdPrincipal = condId,
                    TipoEII = tipoEii,
                    FechaVoto = DateTime.UtcNow
                });
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Carrera de doble-envío contra el UNIQUE filtrado: el voto ya quedó, no es un error.
            }

            return RedirectToPage(new { slug = Slug });
        }

        // Resuelve el ingrediente por slug reusando el match en memoria de la vista pública de
        // ingrediente (catálogo chico y curado). Setea Slug/IngredienteId/Nombre.
        private async Task<bool> ResolverIngredienteAsync(string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return false;
            slug = slug.Trim().ToLowerInvariant();

            var activos = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => i.Activo)
                .Select(i => new { i.Id, i.Nombre })
                .ToListAsync();
            var ing = activos.FirstOrDefault(i => SlugHelper.GenerateSlug(i.Nombre) == slug);
            if (ing == null) return false;

            Slug = slug;
            IngredienteId = ing.Id;
            Nombre = ing.Nombre;
            return true;
        }

        private async Task CargarVotoPropioAsync()
        {
            Guid? uid = UsuarioActual();
            EsAnonimo = uid == null;

            PlatToleraNivel? mi = null;
            if (uid != null)
            {
                mi = await _db.PlatTolerVotos.AsNoTracking()
                    .Where(v => v.IngredienteId == IngredienteId && v.UserId == uid)
                    .Select(v => (PlatToleraNivel?)v.Tolera).FirstOrDefaultAsync();
            }
            else
            {
                var anon = ObtenerAnonId(); // NO crear en GET: solo detectar si ya votó
                if (anon != null)
                    mi = await _db.PlatTolerVotos.AsNoTracking()
                        .Where(v => v.IngredienteId == IngredienteId && v.AnonId == anon)
                        .Select(v => (PlatToleraNivel?)v.Tolera).FirstOrDefaultAsync();
            }

            MiVoto = mi;
            YaVoto = mi != null;
        }

        private async Task CargarResultadosAsync()
        {
            var grupos = await _db.PlatTolerVotos.AsNoTracking()
                .Where(v => v.IngredienteId == IngredienteId)
                .GroupBy(v => v.Tolera)
                .Select(g => new { Nivel = g.Key, Count = g.Count() })
                .ToListAsync();

            CountSi = grupos.FirstOrDefault(g => g.Nivel == PlatToleraNivel.Si)?.Count ?? 0;
            CountAVeces = grupos.FirstOrDefault(g => g.Nivel == PlatToleraNivel.AVeces)?.Count ?? 0;
            CountNo = grupos.FirstOrDefault(g => g.Nivel == PlatToleraNivel.No)?.Count ?? 0;
            TotalRespuestas = CountSi + CountAVeces + CountNo;

            // "A veces" NO entra al binario del porcentaje; se muestra aparte en el desglose.
            var binario = CountSi + CountNo;
            MostrarPorcentaje = TotalRespuestas >= MinVotos && binario > 0;
            if (MostrarPorcentaje)
                PorcentajeTolera = (int)Math.Round((CountSi + 1) / (double)(binario + 2) * 100);
        }

        // Condición principal del usuario (id crudo) + clasificación best-effort a tipo de EII.
        private async Task<(int? CondId, byte? Tipo)> ResolverCondicionAsync(Guid uid)
        {
            var cond = await _db.condicionUsuario.AsNoTracking()
                .Where(c => c.idUsuario == uid && !c.Eliminado && c.idCondicion != null)
                .OrderByDescending(c => c.EsPrincipal)
                .ThenByDescending(c => c.fechaCreado)
                .Select(c => c.idCondicion)
                .FirstOrDefaultAsync();
            if (cond == null) return (null, null);

            var nombre = await _db.condiciones.AsNoTracking()
                .Where(x => x.id == cond.Value)
                .Select(x => x.nombre)
                .FirstOrDefaultAsync();

            return (cond, ClasificarTipoEii(nombre));
        }

        // Denormalización de conveniencia (recomputable desde CondicionIdPrincipal). 1=CUCI, 2=Crohn.
        private static byte? ClasificarTipoEii(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            var n = nombre.ToLowerInvariant();
            if (n.Contains("crohn")) return 2;
            if (n.Contains("colitis") || n.Contains("cuci") || n.Contains("ulcerosa")) return 1;
            return null;
        }

        private static string? NormalizarIntent(string? intent)
        {
            if (string.IsNullOrWhiteSpace(intent)) return null;
            return intent.Trim().ToLowerInvariant() switch
            {
                "si" or "sí" => "si",
                "aveces" or "a-veces" => "aveces",
                "no" => "no",
                _ => null
            };
        }

        private Guid? UsuarioActual()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false)) return null;
            return Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;
        }

        private Guid? ObtenerAnonId()
        {
            return Request.Cookies.TryGetValue(AnonCookie, out var raw) && Guid.TryParse(raw, out var g)
                ? g : (Guid?)null;
        }

        private Guid ObtenerOCrearAnonId()
        {
            var existente = ObtenerAnonId();
            if (existente is Guid g) return g;

            var nuevo = Guid.NewGuid();
            Response.Cookies.Append(AnonCookie, nuevo.ToString(), new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
            return nuevo;
        }
    }
}
