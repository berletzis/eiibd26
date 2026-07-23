using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Models;
using eiibd26.Models.Platillos;
using eiibd26.Services.Platillos;
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
    // comerlo". El % es la media posterior del modelo Beta-Binomial (#16, ToleranciaBayes), NO una
    // cifra clínica; siempre se muestra junto a su intervalo creíble y a la n.
    //
    // Alcance público (#16 §7): SOLO el agregado "Todos". El desglose por tipo de EII vive únicamente
    // en el panel admin — un "X % de pacientes con Crohn toleran…" puede cambiar lo que come alguien
    // enfermo y su n de segmento es mucho menor (los votos anónimos no tienen TipoEII).
    [AllowAnonymous]
    [EnableRateLimiting("tolero")]
    public class EncuestaModel : PageModel
    {
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

        /// <summary>
        /// El ingrediente ya está (activo) en la lista personal "No tolerados" del usuario logueado.
        /// Rige qué muestra el CTA del voto "No": el botón de agregar, o el estado "ya está en tu lista".
        /// </summary>
        public bool YaEnNoTolerados { get; private set; }

        // Opción resaltada por el link de correo (?intent=si|aveces|no). NUNCA vota sola (anti-prefetch).
        public string? IntentDestacado { get; private set; }

        // Resultado de la comunidad.
        public int CountSi { get; private set; }
        public int CountAVeces { get; private set; }
        public int CountNo { get; private set; }
        public int TotalRespuestas { get; private set; }
        public bool MostrarPorcentaje { get; private set; }

        /// <summary>Media posterior redondeada — el "X % lo tolera bien".</summary>
        public int PorcentajeTolera { get; private set; }
        /// <summary>Extremos del intervalo creíble al 95%. Se muestran SIEMPRE junto al porcentaje.</summary>
        public int CiBajo { get; private set; }
        public int CiAlto { get; private set; }

        [TempData] public string? ErrorVoto { get; set; }

        /// <summary>Feedback tras "agregar a mis no tolerados" (PRG). "✓ Agregado…" o "Ya está…".</summary>
        [TempData] public string? FeedbackNoTolerado { get; set; }

        public async Task<IActionResult> OnGetAsync(string? slug, string? intent)
        {
            if (!await ResolverIngredienteAsync(slug)) return NotFound();

            IntentDestacado = NormalizarIntent(intent);
            await CargarVotoPropioAsync();
            await CargarEstadoNoToleradoAsync();
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

        // Agregar el ingrediente a la lista personal "No tolerados" (PlatPerfilExclusion).
        // Acción EXPLÍCITA, solo por POST: un voto comunitario "No" no excluye por sí solo; el usuario
        // toca este botón. Solo logueados (la lista es por idUsuario). Mismo upsert con soft-delete que
        // UsuarioAlimentacion.OnPostAgregarExclusionAsync — reactivar antes que duplicar. Patrón PRG.
        public async Task<IActionResult> OnPostAgregarNoToleradoAsync(string? slug)
        {
            if (!await ResolverIngredienteAsync(slug)) return NotFound();

            Guid? uid = UsuarioActual();
            if (uid == null)
                // Sin sesión no hay lista donde guardar: al login y de vuelta a esta encuesta.
                return Redirect("/Identity/Account/Login?ReturnUrl=" + Uri.EscapeDataString($"/tolero/{Slug}"));

            // Una fila por (usuario, tipo, refId) — activa o borrada. Respeta el único filtrado
            // WHERE Eliminado = 0: si existe borrada se REVIVE, no se inserta otra.
            var fila = await _db.PlatPerfilExclusiones
                .FirstOrDefaultAsync(e => e.idUsuario == uid.Value
                                       && e.Tipo == "Ingrediente"
                                       && e.RefId == IngredienteId);

            if (fila != null)
            {
                if (fila.Eliminado)
                {
                    fila.Eliminado = false;
                    fila.FechaEliminado = null;
                    fila.FechaCreacion = DateTime.UtcNow;
                    FeedbackNoTolerado = "✓ Agregado a tus no tolerados";
                }
                else
                {
                    FeedbackNoTolerado = "Ya está en tu lista";
                }
            }
            else
            {
                _db.PlatPerfilExclusiones.Add(new PlatPerfilExclusion
                {
                    idUsuario = uid.Value,
                    Tipo = "Ingrediente",
                    RefId = IngredienteId,
                    FechaCreacion = DateTime.UtcNow,
                    Eliminado = false
                });
                FeedbackNoTolerado = "✓ Agregado a tus no tolerados";
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Carrera de doble-clic contra el UNIQUE filtrado: la exclusión ya quedó, no es un error.
                FeedbackNoTolerado = "Ya está en tu lista";
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

        // ¿El ingrediente ya está (activo) en la lista personal del usuario? Solo aplica a logueados
        // (la lista es por idUsuario). Decide si el CTA del voto "No" muestra el botón o el estado
        // "ya está en tu lista".
        private async Task CargarEstadoNoToleradoAsync()
        {
            Guid? uid = UsuarioActual();
            if (uid == null) return;

            YaEnNoTolerados = await _db.PlatPerfilExclusiones.AsNoTracking()
                .AnyAsync(e => e.idUsuario == uid.Value
                            && e.Tipo == "Ingrediente"
                            && e.RefId == IngredienteId
                            && !e.Eliminado);
        }

        // Resultado comunitario "Todos" — vía el helper COMPARTIDO, para que /tolero y la ficha de
        // ingrediente jamás muestren cifras distintas. El cálculo (posterior + gate) no vive aquí.
        private async Task CargarResultadosAsync()
        {
            var r = await ToleranciaResultadoCalculo.ParaIngredienteAsync(_db, IngredienteId);
            CountSi = r.CountSi;
            CountAVeces = r.CountAVeces;
            CountNo = r.CountNo;
            TotalRespuestas = r.TotalRespuestas;
            MostrarPorcentaje = r.MostrarPorcentaje;
            PorcentajeTolera = r.PorcentajeTolera;
            CiBajo = r.CiBajo;
            CiAlto = r.CiAlto;
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
